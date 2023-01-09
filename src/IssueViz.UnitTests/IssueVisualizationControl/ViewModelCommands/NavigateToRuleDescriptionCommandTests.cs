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
using SonarLint.VisualStudio.IssueVisualization.Helpers;
using SonarLint.VisualStudio.IssueVisualization.IssueVisualizationControl.ViewModels.Commands;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.IssueVisualizationControl.ViewModelCommands
{
    [TestClass]
    public class NavigateToRuleDescriptionCommandTests
    {
        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow(1)]
        [DataRow(true)]
        public void CanExecute_InvalidRuleKey_False(object parameter)
        {
            var testSubject = CreateTestSubject();

            var result = testSubject.CanExecute(parameter);

            result.Should().BeFalse();
        }

        [TestMethod]
        public void CanExecute_ValidRuleKey_True()
        {
            var testSubject = CreateTestSubject();

            var result = testSubject.CanExecute("some key");

            result.Should().BeTrue();
        }

        [TestMethod]
        public void Execute_RuleDocumentationShown()
        {
            var browserService = new Mock<IShowInBrowserService>();
            var testSubject = CreateTestSubject(browserService.Object);

            testSubject.Execute("some key");

            browserService.Verify(x => x.ShowRuleDescription("some key"), Times.Once);
            browserService.VerifyNoOtherCalls();
        }

        private NavigateToRuleDescriptionCommand CreateTestSubject(IShowInBrowserService browserService = null)
        {
            return new NavigateToRuleDescriptionCommand(browserService);
        }
    }
}
