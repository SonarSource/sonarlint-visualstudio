using System.IO;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarJsConfig;

namespace SonarJSPocTests
{
    [TestClass]
    public class SonarJSDownloaderTests
    {
        [TestMethod]
        public void DownloadAndExtract()
        {
            var downloader = new SonarJsConfig.SonarJSDownloader();
            var url = "https://binaries.sonarsource.com/Distribution/sonar-javascript-plugin/sonar-javascript-plugin-6.2.0.12043.jar";
            var outputDir = downloader.Download(url, new ConsoleLogger());

            outputDir.Should().NotBeNullOrEmpty();
            Directory.Exists(outputDir).Should().BeTrue();
        }
    }
}
