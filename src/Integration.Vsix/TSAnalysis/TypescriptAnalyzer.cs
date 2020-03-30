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

    // TODO:
    // * launch server and capture port
    //      * assume we want launch the server, capture the output streams, and leave it running in the background.
    // * check how parsing errors are handled (?don't want to display them to the user?)
    //      * understand set of possible server error codes: e.g. missing TS, missing node, wrong version of node
    // * experiment with passing "fileContent" instead/as well as "path"
    

    // * investigate embedding artefacts in the VSIX: likely size increase if we embed e.g. the minimum required bits, TypeScript, node
    // * discover list of available rule keys: from files in the jar? API calls at build time?
    // * location of node in VS / location/existence of TS?
    // * number of Java-based vs Node-based rules

    [Export(typeof(IAnalyzer))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class TypescriptAnalyzer : IAnalyzer
    {
        const int Default_ESLintBridge_Port = 50487;

        private readonly int port;
        private readonly ILogger logger;

        [ImportingConstructor]
        public TypescriptAnalyzer(ILogger logger)
            :this(Default_ESLintBridge_Port, logger)
        {
        }

        internal TypescriptAnalyzer(int port, ILogger logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.port = port;
        }

        public bool IsAnalysisSupported(IEnumerable<AnalysisLanguage> languages)
        {
            if (languages == null)
            {
                throw new ArgumentNullException(nameof(languages));
            }

            return languages.Contains(AnalysisLanguage.Javascript) ||
                languages.Contains(AnalysisLanguage.Typescript);
        }

        public void ExecuteAnalysis(string path, string charset, IEnumerable<AnalysisLanguage> detectedLanguages,
            IIssueConsumer consumer,
            ProjectItem projectItem)
        {
            if (!IsAnalysisSupported(detectedLanguages))
            {
                throw new ArgumentOutOfRangeException($"Unsupported language");
            }

            var serverEndpoint = detectedLanguages.Contains(AnalysisLanguage.Typescript) ?
                "analyze-ts" : "analyze-js";

            using var httpClient = new System.Net.Http.HttpClient();
            var request = new
            {
                filePath = path,
                rules = new[]
                {
                    // NOTE: the rule keys we pass to the eslint-bridge are not the Sonar "Sxxxx" keys.
                    // Instead, there are more user-friendly keys.
                    // We will need to translate between the "Sxxx" and the "friendly" keys.
                    // The "friendly" keys are at https://github.com/SonarSource/eslint-plugin-sonarjs/blob/master/src/index.ts
                    new { key = "max-switch-cases", configurations = new string[0] },
                    new { key = "no-duplicate-string", configurations = new string[0] },
                    new { key = "todo-tag", configurations = new string[0] }
                }
            };
 
            var serializedRequest = JsonConvert.SerializeObject(request);

            // If the server can't find typescript then response will contain "MISSING_TYPESCRIPT".
            // To fix this, either:
            // 1) add the "typescript" under the "node_modules" folder of the eslint-bridge server, or
            // 2) set the environment variable NODE_PATH to the a "node_modules" folder that contains "typescript".
            //      NB the variable must be set before launching the server.
            HttpResponseMessage response = null;

            try
            {
                response = httpClient.PostAsync($"http://localhost:{port}/{serverEndpoint}",
                        new StringContent(serializedRequest, Encoding.UTF8, "application/json"))
                    .Result;
            }
            catch(AggregateException ex)
            {
                logger.WriteLine($"Error connecting to the eslint-bridge server. Please ensure the server is running on port {port}");
                logger.WriteLine(ex.ToString());
                foreach(var inner in ex.InnerExceptions)
                {
                    logger.WriteLine("   -------------------------------");
                    logger.WriteLine(inner.ToString());
                }
                return;
            }

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
