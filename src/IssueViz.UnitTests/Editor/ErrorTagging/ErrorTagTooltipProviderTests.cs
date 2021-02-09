/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2021 SonarSource SA
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

using System.Linq;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using FluentAssertions;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarLint.VisualStudio.IssueVisualization.Editor.ErrorTagging;
using Color = System.Drawing.Color;
using IVsThemeColorProvider = SonarLint.VisualStudio.Infrastructure.VS.IVsThemeColorProvider;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.ErrorTagging
{
    [TestClass]
    public class ErrorTagTooltipProviderTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<ErrorTagTooltipProvider, IErrorTagTooltipProvider>(null,
                new[]
                {
                    MefTestHelpers.CreateExport<IVsBrowserService>(Mock.Of<IVsBrowserService>()),
                    MefTestHelpers.CreateExport<IVsThemeColorProvider>(Mock.Of<IVsThemeColorProvider>())
                });
        }

        [TestMethod]
        public void Create_CreatesTooltipWithHyperlink()
        {
            var issue = new Mock<IAnalysisIssueBase>();
            issue.Setup(x => x.RuleKey).Returns("some rule");
            issue.Setup(x => x.Message).Returns("some message");

            var ruleHelpLinkProvider = new Mock<IRuleHelpLinkProvider>();
            ruleHelpLinkProvider.Setup(x => x.GetHelpLink("some rule")).Returns("some url");

            var browserService = new Mock<IVsBrowserService>();

            var testSubject = new ErrorTagTooltipProvider(browserService.Object, Mock.Of<IVsThemeColorProvider>(), ruleHelpLinkProvider.Object);
            var result = testSubject.Create(issue.Object);

            result.Should().NotBeNull();
            result.Should().BeOfType<TextBlock>();

            var inlines = ((TextBlock)result).Inlines.ToList();
            inlines[0].Should().BeOfType<Hyperlink>();
            inlines[1].Should().BeOfType<Run>();
            inlines[2].Should().BeOfType<Run>();

            (inlines[1] as Run).Text.Should().Be(": ");
            (inlines[2] as Run).Text.Should().Be("some message");

            var hyperlink = (Hyperlink)inlines[0];
            hyperlink.Command.Should().NotBeNull();
            hyperlink.Command.CanExecute(null).Should().BeTrue();

            browserService.Invocations.Count.Should().Be(0);
            hyperlink.Command.Execute(null);
            browserService.Verify(x=> x.Navigate("some url"), Times.Once);
            browserService.VerifyNoOtherCalls();
        }
    }
}
