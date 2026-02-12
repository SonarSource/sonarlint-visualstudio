/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using SonarLint.VisualStudio.Core.Binding;

namespace SonarLint.VisualStudio.Core.UnitTests
{
    [TestClass]
    public class RuleHelpLinkProviderTests
    {
        private IActiveSolutionBoundTracker activeSolutionBoundTracker;
        private RuleHelpLinkProvider testSubject;

        [TestInitialize]
        public void TestInitialize()
        {
            activeSolutionBoundTracker = Substitute.For<IActiveSolutionBoundTracker>();
            testSubject = new RuleHelpLinkProvider(activeSolutionBoundTracker);
        }

        [TestMethod]
        public void GetHelpLink_NotBound_ReturnsNull()
        {
            activeSolutionBoundTracker.CurrentConfiguration.Returns(BindingConfiguration.Standalone);

            var ruleId = new SonarCompositeRuleId("csharpsquid", "S1234");

            var result = testSubject.GetHelpLink(ruleId);

            result.Should().BeNull();
        }

        [TestMethod]
        public void GetHelpLink_NullConfiguration_ReturnsNull()
        {
            activeSolutionBoundTracker.CurrentConfiguration.Returns((BindingConfiguration)null);

            var ruleId = new SonarCompositeRuleId("csharpsquid", "S1234");

            var result = testSubject.GetHelpLink(ruleId);

            result.Should().BeNull();
        }

        [TestMethod]
        [DataRow("csharpsquid", "S1234", "cs")]
        [DataRow("roslyn.sonaranalyzer.security.cs", "S2076", "cs")]
        [DataRow("javascript", "S123", "js")]
        [DataRow("cpp", "S456", "cpp")]
        public void GetHelpLink_BoundToSonarCloud_ReturnsCorrectUrl(string repoKey, string ruleKey, string expectedLanguageKey)
        {
            var sonarCloud = new ServerConnection.SonarCloud("my-org", CloudServerRegion.Eu);
            var boundProject = new BoundServerProject("local", "server-project", sonarCloud);
            var configuration = BindingConfiguration.CreateBoundConfiguration(boundProject, SonarLintMode.Connected, "c:\\config");
            activeSolutionBoundTracker.CurrentConfiguration.Returns(configuration);

            var ruleId = new SonarCompositeRuleId(repoKey, ruleKey);

            var result = testSubject.GetHelpLink(ruleId);

            result.Should().Be($"https://sonarcloud.io/organizations/my-org/rules?languages={expectedLanguageKey}&open={repoKey}%3A{ruleKey}&q={ruleKey}");
        }

        [TestMethod]
        [DataRow("csharpsquid", "S1234", "cs")]
        [DataRow("roslyn.sonaranalyzer.security.cs", "S2076", "cs")]
        [DataRow("javascript", "S123", "js")]
        [DataRow("cpp", "S456", "cpp")]
        public void GetHelpLink_BoundToSonarQube_ReturnsCorrectUrl(string repoKey, string ruleKey, string expectedLanguageKey)
        {
            var sonarQube = new ServerConnection.SonarQube(new Uri("https://mysonarqube.com/"));
            var boundProject = new BoundServerProject("local", "server-project", sonarQube);
            var configuration = BindingConfiguration.CreateBoundConfiguration(boundProject, SonarLintMode.Connected, "c:\\config");
            activeSolutionBoundTracker.CurrentConfiguration.Returns(configuration);

            var ruleId = new SonarCompositeRuleId(repoKey, ruleKey);

            var result = testSubject.GetHelpLink(ruleId);

            result.Should().Be($"https://mysonarqube.com/coding_rules?languages={expectedLanguageKey}&open={repoKey}%3A{ruleKey}&q={ruleKey}");
        }
    }
}
