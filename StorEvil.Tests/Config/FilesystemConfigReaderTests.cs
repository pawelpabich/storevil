using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using Rhino.Mocks;

namespace StorEvil.Config
{
    [TestFixture]
    public class FilesystemConfigReaderTests
    {
        private FilesystemConfigReader FilesystemConfigReader;

        private IFilesystem FakeFilesystem;
        private IConfigFileReader FakeFileReader;

        private const string FakeConfigFileContents = "foo";

        [SetUp]
        public void SetupContext()
        {
            FakeFilesystem = MockRepository.GenerateMock<IFilesystem>();
            FakeFileReader = MockRepository.GenerateMock<IConfigFileReader>();

            FilesystemConfigReader = new FilesystemConfigReader(FakeFilesystem, FakeFileReader);
        }

        [Test]
        public void when_config_present_in_same_folder_uses_it()
        {
            ConfigFileExistsAt("c:\\test\\storevil.config");

            ShouldReturnConfigFileWhenCalledFrom("c:\\test\\");
        }

        [Test]
        public void when_no_config_present_in_same_folder_searches_up_the_tree()
        {
            // note: should make successive calls up the directory structure
            // first \test\foo\bar, then \test\foo, then \test

            ConfigFileExistsAt("c:\\test\\storevil.config");
            ShouldReturnConfigFileWhenCalledFrom("c:\\test\\foo\\bar\\");
        }

        private void ShouldReturnConfigFileWhenCalledFrom(string workingDirectory)
        {
            var settings = new ConfigSettings();

            ParserReturnsConfigSettings(settings);

            var result = FilesystemConfigReader.GetConfig(workingDirectory);

            Assert.That(result, Is.SameAs(settings));
        }

        [Test]
        public void when_no_config_present_in_tree_returns_default()
        {
            const string path = "c:\\test\\storevil.config";

            var result = FilesystemConfigReader.GetConfig(path);
            Assert.That(result, Is.Not.Null);
        }

        private void ConfigFileExistsAt(string path)
        {
            FileExists(path);
            FilesystemReturnsConfigFileContents(path);
        }

        private void FileExists(string path)
        {
            FakeFilesystem.Stub(x => x.FileExists(path)).Return(true);
        }

        private void FilesystemReturnsConfigFileContents(string path)
        {
            FakeFilesystem.Stub(x => x.GetFileText(path)).Return(FakeConfigFileContents);
        }

        private void ParserReturnsConfigSettings(ConfigSettings settings)
        {
            FakeFileReader.Stub(x => x.Read(FakeConfigFileContents)).Return(settings);
        }
    }
}