/*
 * SonarQube Client
 * Copyright (C) 2016-2018 SonarSource SA
 * mailto:info AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

using System;
using System.Linq;
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
    public class SonarQubeService_GetRoslynExportProfileAsync : SonarQubeService_TestBase
    {
        [TestMethod]
        public async Task GetRoslynQualityProfile_Old_ExampleFromSonarQube()
        {
            await ConnectToSonarQube();

            SetupRequest("api/qualityprofiles/export?language=cs&name=quality_profile&organization=my-org&exporterKey=roslyn-cs",
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

            var result = await service.GetRoslynExportProfileAsync("quality_profile", "my-org", SonarQubeLanguage.CSharp,
                CancellationToken.None);

            messageHandler.VerifyAll();

            result.Should().NotBeNull();
            result.Configuration.Should().NotBeNull();
            result.Configuration.RuleSet.Should().NotBeNull();
            result.Configuration.RuleSet.GetAttribute("Name").Should().Be("Rules for SonarQube");
            result.Configuration.RuleSet.GetAttribute("Description").Should().Be("This rule set was automatically generated from SonarQube.");
            result.Configuration.RuleSet.GetAttribute("ToolsVersion").Should().Be("14.0");
            result.Configuration.RuleSet.GetElementsByTagName("Rules").Count.Should().Be(1);
            result.Configuration.RuleSet.GetElementsByTagName("Rule").Count.Should().Be(1);
            result.Configuration.AdditionalFiles.Select(x => x.FileName).Should().BeEquivalentTo(new[] { "SonarLint.xml" });

            result.Deployment.Should().NotBeNull();
            result.Deployment.NuGetPackages.Select(x => x.Id).Should().BeEquivalentTo(new[] { "SonarAnalyzer.CSharp" });
            result.Deployment.NuGetPackages.Select(x => x.Version).Should().BeEquivalentTo(new[] { "6.4.0.3322" });
        }

        [TestMethod]
        public async Task GetRoslynQualityProfile_New_ExampleFromSonarQube()
        {
            await ConnectToSonarQube("6.6.0.0");

            SetupRequest("api/qualityprofiles/export?qualityProfile=quality_profile&language=cs&organization=my-org&exporterKey=roslyn-cs",
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

            var result = await service.GetRoslynExportProfileAsync("quality_profile", "my-org", SonarQubeLanguage.CSharp,
                CancellationToken.None);

            messageHandler.VerifyAll();

            result.Should().NotBeNull();
            result.Configuration.Should().NotBeNull();
            result.Configuration.RuleSet.Should().NotBeNull();
            result.Configuration.RuleSet.GetAttribute("Name").Should().Be("Rules for SonarQube");
            result.Configuration.RuleSet.GetAttribute("Description").Should().Be("This rule set was automatically generated from SonarQube.");
            result.Configuration.RuleSet.GetAttribute("ToolsVersion").Should().Be("14.0");
            result.Configuration.RuleSet.GetElementsByTagName("Rules").Count.Should().Be(1);
            result.Configuration.RuleSet.GetElementsByTagName("Rule").Count.Should().Be(1);
            result.Configuration.AdditionalFiles.Select(x => x.FileName).Should().BeEquivalentTo(new[] { "SonarLint.xml" });

            result.Deployment.Should().NotBeNull();
            result.Deployment.NuGetPackages.Select(x => x.Id).Should().BeEquivalentTo(new[] { "SonarAnalyzer.CSharp" });
            result.Deployment.NuGetPackages.Select(x => x.Version).Should().BeEquivalentTo(new[] { "6.4.0.3322" });
        }

        [TestMethod]
        public async Task GetRoslynQualityProfile_NotFound()
        {
            await ConnectToSonarQube();

            SetupRequest("api/qualityprofiles/export?language=cs&name=quality_profile&organization=my-org&exporterKey=roslyn-cs",
                "", HttpStatusCode.NotFound);

            Func<Task<RoslynExportProfileResponse>> func = async () =>
                await service.GetRoslynExportProfileAsync("quality_profile", "my-org", SonarQubeLanguage.CSharp,
                    CancellationToken.None);

            func.Should().ThrowExactly<HttpRequestException>().And
                .Message.Should().Be("Response status code does not indicate success: 404 (Not Found).");

            messageHandler.VerifyAll();
        }
    }
}
