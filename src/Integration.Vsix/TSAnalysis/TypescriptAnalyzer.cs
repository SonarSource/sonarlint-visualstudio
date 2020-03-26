using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Net.Http;
using System.Text;
using EnvDTE;
using Newtonsoft.Json;
using Sonarlint;
using SonarLint.VisualStudio.Integration.Vsix.Analysis;

namespace SonarLint.VisualStudio.Integration.Vsix.TSAnalysis
{
    [Export(typeof(IAnalyzer))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class TypescriptAnalyzer : IAnalyzer
    {
        public bool IsAnalysisSupported(IEnumerable<AnalysisLanguage> languages)
        {
            return languages.Contains(AnalysisLanguage.Javascript);
        }

        public void ExecuteAnalysis(string path, string charset, IEnumerable<AnalysisLanguage> detectedLanguages,
            IIssueConsumer consumer,
            ProjectItem projectItem)
        {
            var port = 62878;
            using var httpClient = new System.Net.Http.HttpClient();
            var request = new
            {
                filePath = path,
                rules = new[]
                {
                    new {key = "S3516", configurations = new string[0]}
                }
            };
            var serializedRequest = JsonConvert.SerializeObject(request);

            var response = httpClient.PostAsync($"http://localhost:{port}/analyze-ts",
                    new StringContent(serializedRequest, Encoding.UTF8, "application/json"))
                .Result;

            var responseString = response.Content.ReadAsStringAsync().Result;

            var eslintBridgeResponse = JsonConvert.DeserializeObject<EslintBridgeResponse>(responseString);

            var analysisIssues = eslintBridgeResponse.Issues.Select(x =>
                new Issue
                {
                    EndLine = x.EndLine ?? 0,
                    Message = x.Message,
                    RuleKey = "javascript:" + x.RuleId,
                    StartLine = x.Line,
                    FilePath = path
                });
            consumer.Accept(path, analysisIssues);
        }
    }


    public class EslintBridgeResponse
    {
        [JsonProperty("issues")]
        public IEnumerable<EslintBridgeIssue> Issues { get; set; }
    }

    public class EslintBridgeIssue
    {
        [JsonProperty("column")]
        public int Column { get; set; }

        [JsonProperty("line")]
        public int Line { get; set; }

        [JsonProperty("endColumn")]
        public int? EndColumn { get; set; }

        [JsonProperty("endLine")]
        public int? EndLine { get; set; }

        [JsonProperty("ruleId")]
        public string RuleId { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("cost")]
        public int? Cost { get; set; }
    }
}
