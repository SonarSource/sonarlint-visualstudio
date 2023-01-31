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
        public void Ctor_GetsToolWindowFromService()
        {
            var toolWindowService = new Mock<IToolWindowService>();

            _ = CreateEducation(toolWindowService: toolWindowService.Object);

            toolWindowService.Verify(x => x.GetToolWindow<RuleDescriptionToolWindow, IRuleDescriptionToolWindow>(), Times.Once);
        }

        [TestMethod]
        public void ShowRuleDescription_EverythingGetsCalledCorrectly()
        {
            var ruleMetaDataProvider = new Mock<IRuleMetadataProvider>();
            var language = Language.Unknown;
            var ruleKey = "key";

            var ruleHelp = Mock.Of<IRuleHelp>();
            ruleMetaDataProvider.Setup(x => x.GetRuleHelp(language, ruleKey)).Returns(ruleHelp);

            var flowDocument = Mock.Of<FlowDocument>();
            var ruleHelpXamlBuilder = new Mock<IRuleHelpXamlBuilder>();
            ruleHelpXamlBuilder.Setup(x => x.Create(ruleHelp)).Returns(flowDocument);

            var ruleDescriptionToolWindow = new Mock<IRuleDescriptionToolWindow>();

            var toolWindowService = new Mock<IToolWindowService>();
            toolWindowService.Setup(x => x.GetToolWindow<RuleDescriptionToolWindow, IRuleDescriptionToolWindow>()).Returns(ruleDescriptionToolWindow.Object);

            var testSubject = CreateEducation(toolWindowService: toolWindowService.Object, ruleMetadataProvider: ruleMetaDataProvider.Object, ruleHelpXamlBuilder: ruleHelpXamlBuilder.Object);
            testSubject.ShowRuleDescription(language, ruleKey);

            ruleMetaDataProvider.Verify(x => x.GetRuleHelp(language, ruleKey), Times.Once);
            ruleHelpXamlBuilder.Verify(x => x.Create(ruleHelp), Times.Once);
            ruleDescriptionToolWindow.Verify(x => x.UpdateContent(flowDocument), Times.Once);
            toolWindowService.Verify(x => x.Show(RuleDescriptionToolWindow.ToolWindowId), Times.Once);
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
