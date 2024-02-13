/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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
using System.Threading;
using System.Windows.Documents;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Education.Commands;
using SonarLint.VisualStudio.Education.Rule;
using SonarLint.VisualStudio.Education.XamlGenerator;
using SonarLint.VisualStudio.TestInfrastructure;

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
                MefTestHelpers.CreateExport<IRuleMetaDataProvider>(),
                MefTestHelpers.CreateExport<IShowRuleInBrowser>(),
                MefTestHelpers.CreateExport<IRuleHelpXamlBuilder>(),
                MefTestHelpers.CreateExport<ILogger>());
        }

        [TestMethod]
        public void ShowRuleHelp_KnownRule_DocumentIsDisplayedInToolWindow()
        {
            var ruleMetaDataProvider = new Mock<IRuleMetaDataProvider>();
            var ruleId = new SonarCompositeRuleId("repoKey", "ruleKey");

            var ruleInfo = Mock.Of<IRuleInfo>();
            ruleMetaDataProvider.Setup(x => x.GetRuleInfoAsync(It.IsAny<SonarCompositeRuleId>(), It.IsAny<CancellationToken>())).ReturnsAsync(ruleInfo);

            var flowDocument = Mock.Of<FlowDocument>();
            var ruleHelpXamlBuilder = new Mock<IRuleHelpXamlBuilder>();
            ruleHelpXamlBuilder.Setup(x => x.Create(ruleInfo, /* todo */ null)).Returns(flowDocument);

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
            testSubject.ShowRuleHelp(ruleId, null);

            ruleMetaDataProvider.Verify(x => x.GetRuleInfoAsync(ruleId, CancellationToken.None), Times.Once);
            ruleHelpXamlBuilder.Verify(x => x.Create(ruleInfo, /* todo */ null), Times.Once);
            ruleDescriptionToolWindow.Verify(x => x.UpdateContent(flowDocument), Times.Once);
            toolWindowService.Verify(x => x.Show(RuleHelpToolWindow.ToolWindowId), Times.Once);

            showRuleInBrowser.Invocations.Should().HaveCount(0);
        }

        [TestMethod]
        public void ShowRuleHelp_FailedToDisplayRule_RuleIsShownInBrowser()
        {
            var toolWindowService = new Mock<IToolWindowService>();
            var ruleMetadataProvider = new Mock<IRuleMetaDataProvider>();
            var ruleHelpXamlBuilder = new Mock<IRuleHelpXamlBuilder>();
            var showRuleInBrowser = new Mock<IShowRuleInBrowser>();

            var ruleId = new SonarCompositeRuleId("repoKey", "ruleKey");

            var ruleInfo = Mock.Of<IRuleInfo>();
            ruleMetadataProvider.Setup(x => x.GetRuleInfoAsync(It.IsAny<SonarCompositeRuleId>(), It.IsAny<CancellationToken>())).ReturnsAsync(ruleInfo);

            ruleHelpXamlBuilder.Setup(x => x.Create(ruleInfo, /* todo */ null)).Throws(new Exception("some layout error"));

            var testSubject = CreateEducation(
                toolWindowService.Object,
                ruleMetadataProvider.Object,
                showRuleInBrowser.Object,
                ruleHelpXamlBuilder.Object);

            toolWindowService.Reset(); // Called in the constructor, so need to reset to clear the list of invocations

            testSubject.ShowRuleHelp(ruleId, /* todo */ null);

            ruleMetadataProvider.Verify(x => x.GetRuleInfoAsync(ruleId, CancellationToken.None), Times.Once);
            showRuleInBrowser.Verify(x => x.ShowRuleDescription(ruleId), Times.Once);

            // should have attempted to build the rule, but failed
            ruleHelpXamlBuilder.Invocations.Should().HaveCount(1);
            toolWindowService.Invocations.Should().HaveCount(1);
        }

        [TestMethod]
        public void ShowRuleHelp_UnknownRule_RuleIsShownInBrowser()
        {
            var toolWindowService = new Mock<IToolWindowService>();
            var ruleMetadataProvider = new Mock<IRuleMetaDataProvider>();
            var ruleHelpXamlBuilder = new Mock<IRuleHelpXamlBuilder>();
            var showRuleInBrowser = new Mock<IShowRuleInBrowser>();

            var unknownRule = new SonarCompositeRuleId("known", "xxx");
            ruleMetadataProvider.Setup(x => x.GetRuleInfoAsync(unknownRule, It.IsAny<CancellationToken>())).ReturnsAsync((IRuleInfo)null);

            var testSubject = CreateEducation(
                toolWindowService.Object,
                ruleMetadataProvider.Object,
                showRuleInBrowser.Object,
                ruleHelpXamlBuilder.Object);

            toolWindowService.Reset(); // Called in the constructor, so need to reset to clear the list of invocations

            testSubject.ShowRuleHelp(unknownRule, /* todo */ null);

            ruleMetadataProvider.Verify(x => x.GetRuleInfoAsync(unknownRule, CancellationToken.None), Times.Once);
            showRuleInBrowser.Verify(x => x.ShowRuleDescription(unknownRule), Times.Once);

            // Should not have attempted to build the rule
            ruleHelpXamlBuilder.Invocations.Should().HaveCount(0);
            toolWindowService.Invocations.Should().HaveCount(0);
        }

        private Education CreateEducation(IToolWindowService toolWindowService = null,
            IRuleMetaDataProvider ruleMetadataProvider = null,
            IShowRuleInBrowser showRuleInBrowser = null,
            IRuleHelpXamlBuilder ruleHelpXamlBuilder = null)
        {
            toolWindowService ??= Mock.Of<IToolWindowService>();
            ruleMetadataProvider ??= Mock.Of<IRuleMetaDataProvider>();
            showRuleInBrowser ??= Mock.Of<IShowRuleInBrowser>();
            ruleHelpXamlBuilder ??= Mock.Of<IRuleHelpXamlBuilder>();
            var logger = new TestLogger(logToConsole: true);
            var threadHandling = new NoOpThreadHandler();

            return new Education(toolWindowService, ruleMetadataProvider, showRuleInBrowser, logger, ruleHelpXamlBuilder, threadHandling);
        }
    }
}
