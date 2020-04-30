/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.CFamily;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarQube.Client;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Core.UnitTests.CFamily
{
    [TestClass]
    public class CFamilyBindingConfigProviderTests
    {
        [TestMethod]
        public void Ctor_NullService_ArgumentNullException()
        {
            Action act = () => new CFamilyBindingConfigProvider(null, BindingConfiguration.Standalone, Mock.Of<ILogger>());

            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("sonarQubeService");
        }


        [TestMethod]
        public void Ctor_NullBindingConfiguration_ArgumentNullException()
        {
            Action act = () => new CFamilyBindingConfigProvider(Mock.Of<ISonarQubeService>(), null, Mock.Of<ILogger>());

            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("bindingConfiguration");
        }

        [TestMethod]
        public void Ctor_NullLogger_ArgumentNullException()
        {
            Action act = () => new CFamilyBindingConfigProvider(Mock.Of<ISonarQubeService>(), BindingConfiguration.Standalone, null);

            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("logger");
        }

        [TestMethod]
        [DataRow(SonarQubeIssueSeverity.Blocker, IssueSeverity.Blocker)]
        [DataRow(SonarQubeIssueSeverity.Critical, IssueSeverity.Critical)]
        [DataRow(SonarQubeIssueSeverity.Info, IssueSeverity.Info)]
        [DataRow(SonarQubeIssueSeverity.Major, IssueSeverity.Major)]
        [DataRow(SonarQubeIssueSeverity.Minor, IssueSeverity.Minor)]
        [DataRow(SonarQubeIssueSeverity.Unknown, null)]
        public void SeverityEnumConversion_NotUnknown(SonarQubeIssueSeverity sqSeverity, IssueSeverity? expected)
        {
            CFamilyBindingConfigProvider.Convert(sqSeverity).Should().Be(expected);
        }

        [TestMethod]
        public void ConvertRulesToSettings()
        {
            // Arrange
            var qpRules = new List<SonarQubeRule>
            {
                new SonarQubeRule("key1", "repo1", false, SonarQubeIssueSeverity.Blocker, new Dictionary<string, string>()),
                new SonarQubeRule("key2", "repo1", true, SonarQubeIssueSeverity.Critical, new Dictionary<string, string>()),
                new SonarQubeRule("key3", "repo1", false, SonarQubeIssueSeverity.Unknown,
                  new Dictionary<string, string>
                  {
                      { "paramKey1", "paramValue1" },
                      { "paramKey2", "paramValue2" },
                      { "paramKey3", "" }
                  }
                ),
            };

            // Act
            var settings = CFamilyBindingConfigProvider.CreateRulesSettingsFromQPRules(qpRules);

            // Assert
            settings.Rules.Count.Should().Be(3);
            settings.Rules.Keys.Should().BeEquivalentTo("repo1:key1", "repo1:key2", "repo1:key3");

            settings.Rules["repo1:key1"].Level.Should().Be(RuleLevel.Off);
            settings.Rules["repo1:key2"].Level.Should().Be(RuleLevel.On);
            settings.Rules["repo1:key3"].Level.Should().Be(RuleLevel.Off);

            settings.Rules["repo1:key1"].Severity.Should().Be(IssueSeverity.Blocker);
            settings.Rules["repo1:key2"].Severity.Should().Be(IssueSeverity.Critical);
            settings.Rules["repo1:key3"].Severity.Should().BeNull();


            settings.Rules["repo1:key1"].Parameters.Should().BeNull();
            settings.Rules["repo1:key2"].Parameters.Should().BeNull();

            var rule3Params = settings.Rules["repo1:key3"].Parameters;
            rule3Params.Should().NotBeNull();
            rule3Params.Keys.Should().BeEquivalentTo("paramKey1", "paramKey2", "paramKey3");
            rule3Params["paramKey1"].Should().Be("paramValue1");
            rule3Params["paramKey2"].Should().Be("paramValue2");
            rule3Params["paramKey3"].Should().Be("");
        }

        [TestMethod]
        public async Task GetRules_Success()
        {
            // Arrange
            var testLogger = new TestLogger();
            var rules = new List<SonarQubeRule>
            {
                new SonarQubeRule("key1", "repo1", true, SonarQubeIssueSeverity.Major, new Dictionary<string, string>()),
                new SonarQubeRule("key2", "repo2", false,SonarQubeIssueSeverity.Info,
                    new Dictionary<string, string>
                    {
                        {  "p1", "v1" },
                        {  "p2", "v2" }
                    })
            };

            var serviceMock = new Mock<ISonarQubeService>();
            serviceMock.Setup(x => x.GetRulesAsync(true, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => rules);

            var testSubject = new CFamilyBindingConfigProvider(serviceMock.Object, BindingConfiguration.Standalone, testLogger);

            // Act
            var result = await testSubject.GetConfigurationAsync(CreateQp(), Language.Cpp, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<CFamilyBindingConfigFile>();

            var cfamilyConfigFile = (CFamilyBindingConfigFile)result;
            cfamilyConfigFile.RuleSettings.Should().NotBeNull();

            var slvsRules = cfamilyConfigFile.RuleSettings.Rules;
            slvsRules.Should().NotBeNull();
            slvsRules.Keys.Should().BeEquivalentTo("repo1:key1", "repo2:key2");
            slvsRules["repo1:key1"].Level.Should().Be(RuleLevel.On);
            slvsRules["repo2:key2"].Level.Should().Be(RuleLevel.Off);

            slvsRules["repo1:key1"].Severity.Should().Be(IssueSeverity.Major);
            slvsRules["repo2:key2"].Severity.Should().Be(IssueSeverity.Info);

            slvsRules["repo1:key1"].Parameters.Should().BeNull();

            var rule2Params = slvsRules["repo2:key2"].Parameters;
            rule2Params.Should().NotBeNull();
            rule2Params.Keys.Should().BeEquivalentTo("p1", "p2");
            rule2Params["p1"].Should().Be("v1");
            rule2Params["p2"].Should().Be("v2");

            testLogger.AssertNoOutputMessages();
        }

        [DebuggerStepThrough]
        private static SonarQubeQualityProfile CreateQp() =>
            new SonarQubeQualityProfile("key1", "", "", false, DateTime.UtcNow);

        [TestMethod]
        public async Task GetRules_NoData_EmptyResultReturned()
        {
            // Arrange
            var testLogger = new TestLogger();
            var serviceMock = new Mock<ISonarQubeService>();
            serviceMock.Setup(x => x.GetRulesAsync(It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => new List<SonarQubeRule>());

            var testSubject = new CFamilyBindingConfigProvider(serviceMock.Object, BindingConfiguration.Standalone, testLogger);

            // Act
            var result = await testSubject.GetConfigurationAsync(CreateQp(), Language.Cpp, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<CFamilyBindingConfigFile>();

            var cfamilyConfigFile = (CFamilyBindingConfigFile)result;
            cfamilyConfigFile.RuleSettings.Should().NotBeNull();
            cfamilyConfigFile.RuleSettings.Rules.Should().NotBeNull();
            cfamilyConfigFile.RuleSettings.Rules.Count.Should().Be(0);

            testLogger.AssertNoOutputMessages();
        }

        [TestMethod]
        public async Task GetRules_NonCriticalException_IsHandledAndNullResultReturned()
        {
            // Arrange
            var testLogger = new TestLogger();

            var serviceMock = new Mock<ISonarQubeService>();
            serviceMock.Setup(x => x.GetRulesAsync(It.IsAny<bool>(), It.IsAny<string>(), CancellationToken.None))
                .ThrowsAsync(new InvalidOperationException("invalid op"));

            var testSubject = new CFamilyBindingConfigProvider(serviceMock.Object, BindingConfiguration.Standalone, testLogger);

            // Act
            var result = await testSubject.GetConfigurationAsync(CreateQp(), Language.Cpp, CancellationToken.None);

            // Assert
            result.Should().BeNull();
            testLogger.AssertPartialOutputStringExists("invalid op");
        }

        [TestMethod]
        public async Task GetRules_AbortIfCancellationRequested()
        {
            // Arrange
            var testLogger = new TestLogger();

            CancellationTokenSource cts = new CancellationTokenSource();

            var serviceMock = new Mock<ISonarQubeService>();
            serviceMock.Setup(x => x.GetRulesAsync(It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                    {
                        cts.Cancel();
                        return new List<SonarQubeRule>();
                    }) ;

            var testSubject = new CFamilyBindingConfigProvider(serviceMock.Object, BindingConfiguration.Standalone, testLogger);

            // Act
            var result = await testSubject.GetConfigurationAsync(CreateQp(), Language.Cpp, cts.Token);

            // Assert
            result.Should().BeNull();
            testLogger.AssertPartialOutputStringExists(CoreStrings.SonarQubeRequestTimeoutOrCancelled);
        }

        [TestMethod]
        public void GetRules_UnsupportedLanguage_Throws()
        {
            // Arrange
            var testLogger = new TestLogger();

            CancellationTokenSource cts = new CancellationTokenSource();
            var serviceMock = new Mock<ISonarQubeService>();

            var testSubject = new CFamilyBindingConfigProvider(serviceMock.Object, BindingConfiguration.Standalone, testLogger);

            // Act
            Action act = () => testSubject.GetConfigurationAsync(CreateQp(), Language.VBNET, cts.Token).Wait();

            // Assert
            act.Should().ThrowExactly<AggregateException>().And.InnerException.Should().BeOfType<ArgumentOutOfRangeException>();
        }

        [TestMethod]
        public void IsSupported()
        {
            // Arrange
            var testLogger = new TestLogger();
            var serviceMock = new Mock<ISonarQubeService>();
            var testSubject = new CFamilyBindingConfigProvider(serviceMock.Object, BindingConfiguration.Standalone, testLogger);

            // 1. Supported languages
            testSubject.IsLanguageSupported(Language.C).Should().BeTrue();
            testSubject.IsLanguageSupported(Language.Cpp).Should().BeTrue();

            testSubject.IsLanguageSupported(new Language("cpp", "FooXXX", "foo", new SonarQubeLanguage("serverId", "serverName")));

            // 2. Not supported
            testSubject.IsLanguageSupported(Language.CSharp).Should().BeFalse();
            testSubject.IsLanguageSupported(Language.VBNET).Should().BeFalse();
        }
    }
}
