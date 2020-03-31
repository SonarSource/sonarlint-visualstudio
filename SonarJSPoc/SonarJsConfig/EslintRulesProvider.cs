using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace SonarJsConfig
{
    public class EslintRulesProvider
    {
        public const string PluginVersion = "6.2.0.12043";

        private readonly string jarRootDirectory;

        public EslintRulesProvider(string jarRootDirectory)
        {
            this.jarRootDirectory = jarRootDirectory ?? throw new ArgumentNullException(nameof(jarRootDirectory));
            if (!Directory.Exists(jarRootDirectory))
            {
                throw new DirectoryNotFoundException(jarRootDirectory);
            }
        }

        public IEnumerable<EslintRuleInfo> GetTypeScriptRules()
        {
            var rules = Load(@"org\sonar\l10n\typescript\rules\tslint\rules.json")
                .Union(Load(@"org\sonar\l10n\typescript\rules\tslint-sonarts\rules.json"))
                .ToArray();

            return rules;
        }

        public IEnumerable<EslintRuleInfo> GetJavaScriptRules()
        {
            // Note: there are json files for multiple other eslint plugins.
            // Should we execute these to?
            // e.g. angular.json, core.json, ember.json ...
            var rules = Load(@"org\sonar\l10n\javascript\rules\eslint\sonarjs.json")
                .ToArray();

            return rules;
        }

        private IEnumerable<EslintRuleInfo> Load(string relativeFilePath)
        {
            var fullPath = Path.Combine(jarRootDirectory, relativeFilePath);
            var data = JsonConvert.DeserializeObject<EslintRuleInfo[]>(File.ReadAllText(fullPath, Encoding.UTF8));
            return data;
        }

    }
}
