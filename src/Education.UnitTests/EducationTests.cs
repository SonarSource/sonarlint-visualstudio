/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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

using System.Windows.Documents;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Education.Commands;
using SonarLint.VisualStudio.Education.XamlGenerator;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarLint.VisualStudio.Rules;

namespace SonarLint.VisualStudio.Education.UnitTests
{
    [TestClass]
    public class EducationTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<Education, IEducation>(
                MefTestHelpers.CreateExport<IToolWindowService>(),
                MefTestHelpers.CreateExport<ILocalRuleMetadataProvider>(),
                MefTestHelpers.CreateExport<IShowRuleInBrowser>(),
                MefTestHelpers.CreateExport<ILogger>());
        }

        [TestMethod]
        public void ShowRuleHelp_KnownRule_DocumentIsDisplayedInToolWindow()
        {
            var ruleMetaDataProvider = new Mock<ILocalRuleMetadataProvider>();
            var ruleId = new SonarCompositeRuleId("repoKey","ruleKey");

            var ruleInfo = Mock.Of<IRuleInfo>();
            ruleMetaDataProvider.Setup(x => x.GetRuleInfo(It.IsAny<SonarCompositeRuleId>())).Returns(ruleInfo);

            var flowDocument = Mock.Of<FlowDocument>();
            var ruleHelpXamlBuilder = new Mock<IRuleHelpXamlBuilder>();
            ruleHelpXamlBuilder.Setup(x => x.Create(ruleInfo)).Returns(flowDocument);

            var ruleDescriptionToolWindow = new Mock<IRuleHelpToolWindow>();

            var toolWindowService = new Mock<IToolWindowService>();
            toolWindowService.Setup(x => x.GetToolWindow<RuleHelpToolWindow, IRuleHelpToolWindow>()).Returns(ruleDescriptionToolWindow.Object);

            var showRuleInBrowser = new Mock<IShowRuleInBrowser>();
            var testSubject = CreateEducation(toolWindowService.Object,
                ruleMetaDataProvider.Object,
                showRuleInBrowser.Object,
                ruleHelpXamlBuilder.Object);

            // Sanity check - tool window not yet fetched
            toolWindowService.Invocations.Should().HaveCount(0);

            // Act
            testSubject.ShowRuleHelp(ruleId);

            ruleMetaDataProvider.Verify(x => x.GetRuleInfo(ruleId), Times.Once);
            ruleHelpXamlBuilder.Verify(x => x.Create(ruleInfo), Times.Once);
            ruleDescriptionToolWindow.Verify(x => x.UpdateContent(flowDocument), Times.Once);
            toolWindowService.Verify(x => x.Show(RuleHelpToolWindow.ToolWindowId), Times.Once);

            showRuleInBrowser.Invocations.Should().HaveCount(0);
        }

        [TestMethod]
        public void ShowRuleHelp_UnknownRule_RuleIsShownInBrowser()
        {
            var toolWindowService = new Mock<IToolWindowService>();
            var ruleMetadataProvider = new Mock<ILocalRuleMetadataProvider>();
            var ruleHelpXamlBuilder = new Mock<IRuleHelpXamlBuilder>();
            var showRuleInBrowser = new Mock<IShowRuleInBrowser>();

            var unknownRule = new SonarCompositeRuleId("known", "xxx");
            ruleMetadataProvider.Setup(x => x.GetRuleInfo(unknownRule)).Returns((IRuleInfo)null);

            var testSubject = CreateEducation(
                toolWindowService.Object,
                ruleMetadataProvider.Object,
                showRuleInBrowser.Object,
                ruleHelpXamlBuilder.Object);

            toolWindowService.Reset(); // Called in the constructor, so need to reset to clear the list of invocations
            
            testSubject.ShowRuleHelp(unknownRule);

            ruleMetadataProvider.Verify(x => x.GetRuleInfo(unknownRule), Times.Once);
            showRuleInBrowser.Verify(x => x.ShowRuleDescription(unknownRule), Times.Once);

            // Should not have attempted to build the rule
            ruleHelpXamlBuilder.Invocations.Should().HaveCount(0);
            toolWindowService.Invocations.Should().HaveCount(0);
        }

        private Education CreateEducation(IToolWindowService toolWindowService = null,
            ILocalRuleMetadataProvider ruleMetadataProvider = null,
            IShowRuleInBrowser showRuleInBrowser = null,
            IRuleHelpXamlBuilder ruleHelpXamlBuilder = null)
        {
            toolWindowService ??= Mock.Of<IToolWindowService>();
            ruleMetadataProvider ??= Mock.Of<ILocalRuleMetadataProvider>();
            showRuleInBrowser ??= Mock.Of<IShowRuleInBrowser>();
            ruleHelpXamlBuilder ??= Mock.Of<IRuleHelpXamlBuilder>();
            var logger = new TestLogger(logToConsole: true);
            var threadHandling = new NoOpThreadHandler();

            return new Education(toolWindowService, ruleMetadataProvider, showRuleInBrowser, logger, ruleHelpXamlBuilder, threadHandling);
        }
    }
}
