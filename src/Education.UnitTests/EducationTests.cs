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

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Education.Commands;
using SonarLint.VisualStudio.Education.XamlGenerator;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarLint.VisualStudio.Rules;
using System.Windows.Documents;

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
                MefTestHelpers.CreateExport<IRuleMetadataProvider>(),
                MefTestHelpers.CreateExport<ILogger>());
        }

        [TestMethod]
        public void ShowRuleHelp_EverythingGetsCalledCorrectly()
        {
            var ruleMetaDataProvider = new Mock<IRuleMetadataProvider>();
            var ruleId = new SonarCompositeRuleId("repoKey","ruleKey");

            var ruleInfo = Mock.Of<IRuleInfo>();
            ruleMetaDataProvider.Setup(x => x.GetRuleInfo(It.IsAny<SonarCompositeRuleId>())).Returns(ruleInfo);

            var flowDocument = Mock.Of<FlowDocument>();
            var ruleHelpXamlBuilder = new Mock<IRuleHelpXamlBuilder>();
            ruleHelpXamlBuilder.Setup(x => x.Create(ruleInfo)).Returns(flowDocument);

            var ruleDescriptionToolWindow = new Mock<IRuleHelpToolWindow>();

            var toolWindowService = new Mock<IToolWindowService>();
            toolWindowService.Setup(x => x.GetToolWindow<RuleHelpToolWindow, IRuleHelpToolWindow>()).Returns(ruleDescriptionToolWindow.Object);

            var testSubject = CreateEducation(toolWindowService: toolWindowService.Object, ruleMetadataProvider: ruleMetaDataProvider.Object, ruleHelpXamlBuilder: ruleHelpXamlBuilder.Object);
            // Sanity check - tool window not yet fetched
            toolWindowService.Invocations.Should().HaveCount(0);

            // Act
            testSubject.ShowRuleHelp(ruleId);

            ruleMetaDataProvider.Verify(x => x.GetRuleInfo(ruleId), Times.Once);
            ruleHelpXamlBuilder.Verify(x => x.Create(ruleInfo), Times.Once);
            ruleDescriptionToolWindow.Verify(x => x.UpdateContent(flowDocument), Times.Once);
            toolWindowService.Verify(x => x.Show(RuleHelpToolWindow.ToolWindowId), Times.Once);
        }

        [TestMethod]
        public void ShowRuleHelp_UnknownRule_ToolWindowIsNotUpdated()
        {
            var toolWindowService = new Mock<IToolWindowService>();
            var ruleMetadataProvider = new Mock<IRuleMetadataProvider>();
            var logger = new TestLogger(logToConsole: true);
            var ruleHelpXamlBuilder = new Mock<IRuleHelpXamlBuilder>();

            var unknownRule = new SonarCompositeRuleId("known", "xxx");
            ruleMetadataProvider.Setup(x => x.GetRuleInfo(unknownRule)).Returns((IRuleInfo)null);

            var testSubject = CreateEducation(
                toolWindowService.Object,
                ruleMetadataProvider.Object,
                logger,
                ruleHelpXamlBuilder.Object);

            toolWindowService.Reset(); // Called in the constructor, so need to reset to clear the list of invocations
            
            testSubject.ShowRuleHelp(unknownRule);

            ruleMetadataProvider.Verify(x => x.GetRuleInfo(unknownRule), Times.Once);
            ruleHelpXamlBuilder.Invocations.Should().HaveCount(0);
            toolWindowService.Invocations.Should().HaveCount(0);

            logger.AssertPartialOutputStringExists(unknownRule.ErrorListErrorCode);
        }

        private Education CreateEducation(IToolWindowService toolWindowService = null, IRuleMetadataProvider ruleMetadataProvider = null, ILogger logger = null, IRuleHelpXamlBuilder ruleHelpXamlBuilder = null)
        {
            toolWindowService ??= Mock.Of<IToolWindowService>();
            ruleMetadataProvider ??= Mock.Of<IRuleMetadataProvider>();
            logger ??= Mock.Of<ILogger>();
            ruleHelpXamlBuilder ??= Mock.Of<IRuleHelpXamlBuilder>();
            var threadHandling = new NoOpThreadHandler();

            return new Education(toolWindowService, ruleMetadataProvider, logger, ruleHelpXamlBuilder, threadHandling);
        }
    }
}
