using System.Collections.Generic;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarJsConfig;

namespace SonarJSPocTests
{
    [TestClass]
    public class EslintRulesProviderTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void GetTypescriptRules()
        {
            var jarDir = EnsurePluginDownloaded();
            var loader = new EslintRulesProvider(jarDir);

            var actual = loader.GetTypeScriptRules();

            actual.Should().NotBeEmpty();
            
            DumpKeys(actual);
        }

        [TestMethod]
        public void GetJavascriptRules()
        {
            var jarDir = EnsurePluginDownloaded();
            var loader = new EslintRulesProvider(jarDir);

            var actual = loader.GetJavaScriptRules();

            actual.Should().NotBeEmpty();

            DumpKeys(actual);
        }

        private void DumpKeys(IEnumerable<EslintRuleInfo> eslintRuleInfos)
        {
            foreach(var item in eslintRuleInfos)
            {
                TestContext.WriteLine(item.Key);
            }
        }


        private string EnsurePluginDownloaded()
        {
            var downloader = new SonarJsConfig.SonarJSDownloader();
            var url = "https://binaries.sonarsource.com/Distribution/sonar-javascript-plugin/sonar-javascript-plugin-6.2.0.12043.jar";
            var outputDir = downloader.Download(url, new ConsoleLogger());
            return outputDir;
        }
    }
}
