using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using StorEvil.Core;

namespace StorEvil.Parsing
{
    public class StoryParser : IStoryParser
    {
        public Story Parse(string storyText, string id)
        {
            var storyParsingJob = new StoryParsingJob();
            return storyParsingJob.Parse(storyText, id);
        }
    }

    /// <summary>
    /// Parses a story.
    /// Instantiated newly for each story.
    /// (holds the state that is kept as the parsing process runs to construct a story)
    /// </summary>
    public class StoryParsingJob
    {
        private readonly List<IScenario> scenarios = new List<IScenario>();
        private readonly StringBuilder _storyName = new StringBuilder();
        private ScenarioBuildingInfo _currentScenario;
        private string _storyId;

        private Action<string> _currentLineHandler;

        public Story Parse(string storyText, string storyId)
        {
            InitializeParsing(storyId);

            foreach (var line in ParseLines(storyText))
                HandleStoryTextLine(line);

            AddScenarioOrOutlineIfExists();

            FixEmptyScenarioNames();

            return GetStory();
        }

        private void InitializeParsing(string storyId)
        {
            _storyId = storyId;
            _currentLineHandler = AppendToStoryName;
        }       

        private void HandleStoryTextLine(string line)
        {
            if (IsNewScenarioOrOutline(line))
            {
                InitializeNewScenario(line);
                return;
            }

            if (IsStartOfExamples(line))
            {
                _currentLineHandler = HandleScenarioExampleRow;
                return;
            }

            _currentLineHandler(line.Trim());
        }

        private Story GetStory()
        {
            return new Story(_storyId, _storyName.ToString().Trim(), scenarios);
        }

        private static bool IsNewScenarioOrOutline(string line)
        {
            return IsScenarioOutlineHeader(line) || IsScenarioHeader(line);
        }

        private void InitializeNewScenario(string line)
        {
            AddScenarioOrOutlineIfExists();

            _currentScenario = new ScenarioBuildingInfo {Name = line.After(":").Trim()};
            _currentLineHandler = HandleScenarioLine;
        }

        private void AppendToStoryName(string line)
        {
            _storyName.Append(line + " ");
        }

        private void AddScenarioOutline()
        {
            var innerScenario = BuildScenario();

            var count = _currentScenario.RowData.First().Count() - 1;
            var fieldNames = _currentScenario.RowData.First().Take(count);
            var examples =
                _currentScenario.RowData.Skip(1).Select(x => x.Take(count));

            var scenarioOutline = new ScenarioOutline(_storyId + "- outline -" + scenarios.Count,
                                                      _currentScenario.Name,
                                                      innerScenario,
                                                      fieldNames,
                                                      examples);
            scenarios.Add(
                scenarioOutline);
        }

        private Scenario BuildScenario()
        {
            return new Scenario(_storyId + "-" + scenarios.Count,
                                _currentScenario.Name,
                                _currentScenario.Lines);
        }

        private void AddScenarioOrOutlineIfExists()
        {
            if (_currentScenario == null)
                return;

            if (CurrentScenarioIsOutline())
                AddScenarioOutline();
            else
                AddScenario();
        }

        private bool CurrentScenarioIsOutline()
        {
            return _currentScenario.RowData != null &&
                   _currentScenario.RowData.Count() > 0;
        }

        private void AddScenario()
        {
            scenarios.Add(BuildScenario());
        }

        private void HandleScenarioLine(string line)
        {
            if (!IsComment(line))
                _currentScenario.Lines.Add(line);
        }

        private void HandleScenarioExampleRow(string line)
        {
            if (_currentScenario != null && line.StartsWith("|"))
                _currentScenario.RowData.Add(line.Split('|').Skip(1));
        }

        private void FixEmptyScenarioNames()
        {
            foreach (var scenario in scenarios.Where(scenario => string.IsNullOrEmpty(scenario.Name)))
                SetScenarioNameToDefault(scenario);
        }

        private static void SetScenarioNameToDefault(IScenario scenario)
        {
            if (scenario is Scenario)
            {
                var s = scenario as Scenario;
                s.Name = string.Join("\r\n", s.Body.ToArray());
            }
            else if (scenario is ScenarioOutline)
            {
                var s = scenario as ScenarioOutline;
                s.Name = string.Join("\r\n", s.Scenario.Body.ToArray());
            }
        }

        private static bool IsComment(string s)
        {
            return s.Trim().StartsWith("#");
        }

        private static bool IsStartOfExamples(string line)
        {
            return line.ToLower().StartsWith("examples:");
        }

        private static bool IsScenarioHeader(string line)
        {
            return line.ToLower().StartsWith("scenario:");
        }

        private static bool IsScenarioOutlineHeader(string line)
        {
            return line.ToLower().StartsWith("scenario outline:");
        }

        internal class ScenarioBuildingInfo
        {
            public string Name;
            public List<string> Lines = new List<string>();
            public List<IEnumerable<string>> RowData = new List<IEnumerable<string>>();
        }

        private static IEnumerable<string> ParseLines(string text)
        {
            return text.Split(new[] {'\n', '\r'}, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}