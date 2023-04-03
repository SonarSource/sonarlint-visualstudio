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
        [DataRow("rule key in invalid format")]
        [DataRow(":xxx")]
        [DataRow(":123:")]
        [DataRow("xxx:123:asd")]
        public void CanExecute_InvalidRuleKey_False(object parameter)
        {
            var testSubject = CreateTestSubject();

            var result = testSubject.CanExecute(parameter);

            result.Should().BeFalse();
        }

        [TestMethod]
        [DataRow("c:S000")]
        [DataRow("cpp:S111")]
        [DataRow("csharpsquid:S222")]
        [DataRow("vbnet:S333")]
        [DataRow("javascript:S444")]
        [DataRow("typescript:555")]
        public void CanExecute_ValidRuleKey_True(string fullRuleKey)
        {
            var testSubject = CreateTestSubject();

            var result = testSubject.CanExecute(fullRuleKey);

            result.Should().BeTrue();
        }

        [TestMethod]
        [DataRow("c:S000")]
        [DataRow("cpp:S111")]
        [DataRow("csharpsquid:S222")]
        [DataRow("vbnet:S333")]
        [DataRow("javascript:S444")]
        [DataRow("typescript:555")]
        public void Execute_RuleDocumentationShown(string fullRuleKey)
        {
            var educationService = new Mock<IEducation>();
            var testSubject = CreateTestSubject(educationService.Object);

            testSubject.Execute(fullRuleKey);

            educationService.Verify(x => x.ShowRuleHelp(It.IsAny<SonarCompositeRuleId>(), /* todo */ null), Times.Once);
            educationService.VerifyNoOtherCalls();

            var actualRuleId = (SonarCompositeRuleId)educationService.Invocations[0].Arguments[0];
            SonarCompositeRuleId.TryParse(fullRuleKey, out var expectedId);

            actualRuleId.RepoKey.Should().Be(expectedId.RepoKey);
            actualRuleId.RuleKey.Should().Be(expectedId.RuleKey);
        }

        private NavigateToRuleDescriptionCommand CreateTestSubject(IEducation educationService = null)
        {
            educationService ??= Mock.Of<IEducation>();
            return new NavigateToRuleDescriptionCommand(educationService);
        }
    }
}
