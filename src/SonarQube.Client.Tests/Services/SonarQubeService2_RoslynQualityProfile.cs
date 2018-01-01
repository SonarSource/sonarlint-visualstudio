using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarQube.Client.Messages;
using SonarQube.Client.Models;

namespace SonarQube.Client.Tests.Services
{
    [TestClass]
    public class SonarQubeService2_RoslynQualityProfile : SonarQubeService2_TestBase
    {
        [TestMethod]
        public async Task GetRoslynQualityProfile_Old_ExampleFromSonarQube()
        {
            await ConnectToSonarQube();

            SetupRequest("api/qualityprofiles/export?language=cs&profileName=quality_profile&exporterKey=roslyn-cs",
                @"<?xml version=""1.0"" encoding=""utf-8""?>
<RoslynExportProfile Version=""1.0"">
  <Configuration>
    <RuleSet Name=""Rules for SonarQube"" Description=""This rule set was automatically generated from SonarQube."" ToolsVersion=""14.0"">
      <Rules AnalyzerId=""SonarAnalyzer.CSharp"" RuleNamespace=""SonarAnalyzer.CSharp"">
        <Rule Id=""S121"" Action=""Warning"" />
      </Rules>
    </RuleSet>
    <AdditionalFiles>
      <AdditionalFile FileName=""SonarLint.xml"" />
    </AdditionalFiles>
  </Configuration>
  <Deployment>
    <Plugins>
      <Plugin Key=""csharp"" Version=""6.4.0.3322"" StaticResourceName=""SonarAnalyzer-6.4.0.3322.zip"" />
    </Plugins>
    <NuGetPackages>
      <NuGetPackage Id=""SonarAnalyzer.CSharp"" Version=""6.4.0.3322"" />
    </NuGetPackages>
  </Deployment>
</RoslynExportProfile>");

            var result = await service.GetRoslynExportProfileAsync("quality_profile", SonarQubeLanguage.CSharp,
                CancellationToken.None);

            messageHandler.VerifyAll();

            result.Should().NotBeNull();
        }

        [TestMethod]
        public async Task GetRoslynQualityProfile_New_ExampleFromSonarQube()
        {
            await ConnectToSonarQube("6.6.0.0");

            SetupRequest("api/qualityprofiles/export?qualityProfile=quality_profile&language=cs&exporterKey=roslyn-cs",
                @"<?xml version=""1.0"" encoding=""utf-8""?>
<RoslynExportProfile Version=""1.0"">
  <Configuration>
    <RuleSet Name=""Rules for SonarQube"" Description=""This rule set was automatically generated from SonarQube."" ToolsVersion=""14.0"">
      <Rules AnalyzerId=""SonarAnalyzer.CSharp"" RuleNamespace=""SonarAnalyzer.CSharp"">
        <Rule Id=""S121"" Action=""Warning"" />
      </Rules>
    </RuleSet>
    <AdditionalFiles>
      <AdditionalFile FileName=""SonarLint.xml"" />
    </AdditionalFiles>
  </Configuration>
  <Deployment>
    <Plugins>
      <Plugin Key=""csharp"" Version=""6.4.0.3322"" StaticResourceName=""SonarAnalyzer-6.4.0.3322.zip"" />
    </Plugins>
    <NuGetPackages>
      <NuGetPackage Id=""SonarAnalyzer.CSharp"" Version=""6.4.0.3322"" />
    </NuGetPackages>
  </Deployment>
</RoslynExportProfile>");

            var result = await service.GetRoslynExportProfileAsync("quality_profile", SonarQubeLanguage.CSharp,
                CancellationToken.None);

            messageHandler.VerifyAll();

            result.Should().NotBeNull();
        }

        [TestMethod]
        public async Task GetRoslynQualityProfile_NotFound()
        {
            await ConnectToSonarQube();

            SetupRequest("api/qualityprofiles/export?language=cs&profileName=quality_profile&exporterKey=roslyn-cs",
                "", HttpStatusCode.NotFound);

            Func<Task<RoslynExportProfileResponse>> func = async () =>
                await service.GetRoslynExportProfileAsync("quality_profile", SonarQubeLanguage.CSharp,
                    CancellationToken.None);

            func.ShouldThrow<HttpRequestException>().And
                .Message.Should().Be("Response status code does not indicate success: 404 (Not Found).");

            messageHandler.VerifyAll();
        }
    }
}
