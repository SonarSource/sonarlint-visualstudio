/*
 * SonarLint for Visual Studio
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
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.Persistence;
using SonarLint.VisualStudio.Integration.Rules;
using SonarQube.Client.Messages;
using SonarQube.Client.Models;
using SonarQube.Client;

namespace SonarLint.VisualStudio.Integration.UnitTests.Rules
{
    [TestClass]
    public class SonarQubeQualityProfileProviderTests
    {
        private Mock<ILogger> loggerMock = new Mock<ILogger>();
        private Mock<ISonarQubeService> serviceMock = new Mock<ISonarQubeService>();

        private readonly SonarQubeQualityProfile ValidQualityProfileResponse = new SonarQubeQualityProfile(
            "qp.key", "qp.name", Language.VBNET.ToString(), false, DateTime.UtcNow);

        [TestInitialize]
        public void TestInitialize()
        {
            loggerMock = new Mock<ILogger>();
            serviceMock = new Mock<ISonarQubeService>();
        }

        [TestMethod]
        public void Ctor_WhenHasNullArgs_Throws()
        {
            // 1. Null service
            Action act = () => new SonarQubeQualityProfileProvider(null, loggerMock.Object);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("sonarQubeService");

            // 2. Null logger
            act = () => new SonarQubeQualityProfileProvider(serviceMock.Object, null);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");
        }

        [TestMethod]
        public void GetQualityProfile_WhenHasNullArgs_Throws()
        {
            var testSubject = new SonarQubeQualityProfileProvider(serviceMock.Object, loggerMock.Object);

            // 1. Null project
            Action act = () => testSubject.GetQualityProfile(null, Language.CSharp);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("project");

            // 2. Null language
            act = () => testSubject.GetQualityProfile(new BoundSonarQubeProject(), null);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("language");
        }

        [TestMethod]
        public void GetQualityProfile_WhenHasUnrecognisedLanguage_ReturnsNull()
        {
            // Arrange
            var testSubject = new SonarQubeQualityProfileProvider(serviceMock.Object, loggerMock.Object);

            // Act
            var result = testSubject.GetQualityProfile(new BoundSonarQubeProject(), Language.Unknown);

            // Assert
            result.Should().BeNull();
        }

        [TestMethod]
        public void GetQualityProfile_WhenHasNoQualityProfile_ReturnsNull()
        {
            // Arrange
            serviceMock
                .Setup(x => x.GetQualityProfileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SonarQubeLanguage>(), It.IsAny<CancellationToken>()))
                .Throws(new InvalidOperationException("The SonarC# plugin is not installed on the connected SonarQube."));

            var testSubject = new SonarQubeQualityProfileProvider(serviceMock.Object, loggerMock.Object);

            // Act
            var result = testSubject.GetQualityProfile(new BoundSonarQubeProject(), Language.CSharp);

            // Assert
            result.Should().BeNull();
            loggerMock.Verify(
                x => x.WriteLine("SonarQube request failed: {0} {1}", "The SonarC# plugin is not installed on the connected SonarQube.", null),
                Times.Once());
        }

        [TestMethod]
        public void GetQualityProfile_WhenHasNullExportResponse_ReturnsNull()
        {
            // Arrange
            SetupServiceResponses(ValidQualityProfileResponse, null);
            var testSubject = new SonarQubeQualityProfileProvider(serviceMock.Object, loggerMock.Object);

            // Act
            var result = testSubject.GetQualityProfile(new BoundSonarQubeProject(), Language.Unknown);

            // Assert
            result.Should().BeNull();
        }

        [TestMethod]
        public void GetQualityProfile_WhenHasExportResponse_ReturnsExportResponse()
        {
            // Arrange
            XmlDocument rulesetDoc = new XmlDocument();

            var rulesXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Rules for SonarQube"" Description=""This rule set was automatically generated from SonarQube."" ToolsVersion=""14.0"">
  <Rules AnalyzerId=""SonarAnalyzer.CSharp"" RuleNamespace=""SonarAnalyzer.CSharp"">
    <Rule Id=""S121"" Action=""Warning"" />
    <Rule Id=""S122"" Action=""Warning"" />
  </Rules>
</RuleSet>";

            rulesetDoc.LoadXml(rulesXml);
            var exportResponse = new RoslynExportProfileResponse
            {
                Configuration = new ConfigurationResponse
                {
                    RuleSet = rulesetDoc.DocumentElement
                }
            };

            SetupServiceResponses(ValidQualityProfileResponse, exportResponse);
            var testSubject = new SonarQubeQualityProfileProvider(serviceMock.Object, loggerMock.Object);

            // Act
            var result = testSubject.GetQualityProfile(new BoundSonarQubeProject(), Language.VBNET);

            // Assert
            result.Should().NotBeNull();
            result.Rules.Count().Should().Be(2);
        }

        private void SetupServiceResponses(SonarQubeQualityProfile qualityProfileResponse,
            RoslynExportProfileResponse exportResponse)
        {
            serviceMock.Setup(x => x.GetQualityProfileAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<SonarQubeLanguage>(),
                It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<SonarQubeQualityProfile>(qualityProfileResponse));

            serviceMock.Setup(x => x.GetRoslynExportProfileAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<SonarQubeLanguage>(),
                It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<RoslynExportProfileResponse>(exportResponse));
        }
    }
}
