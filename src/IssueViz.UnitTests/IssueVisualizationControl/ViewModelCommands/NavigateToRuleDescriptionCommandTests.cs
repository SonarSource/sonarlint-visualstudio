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

using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.IssueVisualization.IssueVisualizationControl.ViewModels.Commands;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.IssueVisualizationControl.ViewModelCommands
{
    [TestClass]
    public class NavigateToRuleDescriptionCommandTests
    {
        [TestMethod]
        [DataRow(null)]
        [DataRow("cpp:S111")]
        [DataRow(1)]
        [DataRow(true)]
        public void CanExecute_InvalidObjectType_False(object parameter)
        {
            var testSubject = CreateTestSubject();

            var result = testSubject.CanExecute(parameter);

            result.Should().BeFalse();
        }

        [TestMethod]
        [DataRow("c:S000", true)]
        [DataRow("cpp:S111", true)]
        [DataRow("csharpsquid:S222", true)]
        [DataRow("vbnet:S333", true)]
        [DataRow("javascript:S444", true)]
        [DataRow("typescript:555", true)]
        [DataRow("rule key in invalid format", false)]
        [DataRow(":xxx", false)]
        [DataRow(":123:", false)]
        [DataRow("xxx:123:asd", false)]
        [DataRow("", false)]
        public void CanExecute_ValidObjectType_CheckRuleKey(string fullRuleKey, bool expectedResult)
        {
            var testSubject = CreateTestSubject();

            var executeParam = new NavigateToRuleDescriptionCommandParam { FullRuleKey = fullRuleKey };

            var result = testSubject.CanExecute(executeParam);

            result.Should().Be(expectedResult);
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

            var executeParam = new NavigateToRuleDescriptionCommandParam { FullRuleKey = fullRuleKey };

            testSubject.Execute(executeParam);

            educationService.Verify(x => x.ShowRuleHelp(It.IsAny<SonarCompositeRuleId>(), /* todo */ null), Times.Once);
            educationService.VerifyNoOtherCalls();

            var actualRuleId = (SonarCompositeRuleId)educationService.Invocations[0].Arguments[0];
            SonarCompositeRuleId.TryParse(fullRuleKey, out var expectedId);

            actualRuleId.RepoKey.Should().Be(expectedId.RepoKey);
            actualRuleId.RuleKey.Should().Be(expectedId.RuleKey);
        }

        [TestMethod]
        public void Execute_WrongTypeParameter_DoesNotCrash()
        {
            var educationService = new Mock<IEducation>();
            var testSubject = CreateTestSubject(educationService.Object);

            testSubject.Execute("wrong param");

            educationService.VerifyNoOtherCalls();
        }

        private NavigateToRuleDescriptionCommand CreateTestSubject(IEducation educationService = null)
        {
            educationService ??= Mock.Of<IEducation>();
            return new NavigateToRuleDescriptionCommand(educationService);
        }
    }
}
