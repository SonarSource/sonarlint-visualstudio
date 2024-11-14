﻿/*
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

using Microsoft.VisualStudio.Shell.TableControl;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Suppressions;
using SonarLint.VisualStudio.Education.SonarLint.VisualStudio.Education.ErrorList;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Education.UnitTests.ErrorList
{
    [TestClass]
    public class SonarErrorListEventProcessorTests
    {
        [TestMethod]
        public void PreprocessNavigateToHelp_NotASonarRule_EventIsNotHandled()
        {
            SonarCompositeRuleId ruleId = null;
            var handle = Mock.Of<ITableEntryHandle>();
            var errorListHelper = CreateErrorListHelper(isSonarRule: false, ruleId);

            var education = new Mock<IEducation>();
            var eventArgs = new TableEntryEventArgs();

            var testSubject = CreateTestSubject(education.Object, errorListHelper.Object);

            testSubject.PreprocessNavigateToHelp(handle, eventArgs);

            errorListHelper.Verify(x => x.TryGetRuleId(handle, out ruleId));
            education.Invocations.Should().HaveCount(0);
            eventArgs.Handled.Should().BeFalse();
        }

        [TestMethod]
        public void PreprocessNavigateToHelp_IsASonarRule_EventIsHandledAndEducationServiceCalled()
        {
            SonarCompositeRuleId ruleId;
            SonarCompositeRuleId.TryParse("cpp:S123", out ruleId);
            var handle = Mock.Of<ITableEntryHandle>();
            var errorListHelper = CreateErrorListHelper(isSonarRule: true, ruleId);

            var education = new Mock<IEducation>();
            var eventArgs = new TableEntryEventArgs();

            var testSubject = CreateTestSubject(education.Object, errorListHelper.Object);

            testSubject.PreprocessNavigateToHelp(handle, eventArgs);

            errorListHelper.Verify(x => x.TryGetRuleId(handle, out ruleId));
            education.Invocations.Should().HaveCount(1);
            education.Verify(x => x.ShowRuleHelp(ruleId, null, /* todo by SLVS-1630 */ null));
            eventArgs.Handled.Should().BeTrue();
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void PreprocessNavigateToHelp_IsASonarRule_EducationServiceIsCalledWithIssueId(bool getFilterableIssueResult)
        {
            SonarCompositeRuleId ruleId;
            IFilterableIssue filterableIssue = new Mock<IFilterableIssue>().Object;
            SonarCompositeRuleId.TryParse("cpp:S123", out ruleId);
            var handle = Mock.Of<ITableEntryHandle>();
            var errorListHelper = CreateErrorListHelper(isSonarRule: true, ruleId);
            errorListHelper.Setup(x => x.TryGetFilterableIssue(It.IsAny<ITableEntryHandle>(), out filterableIssue)).Returns(getFilterableIssueResult);
            var education = new Mock<IEducation>();
            var testSubject = CreateTestSubject(education.Object, errorListHelper.Object);

            testSubject.PreprocessNavigateToHelp(handle, new TableEntryEventArgs());

            education.Invocations.Should().HaveCount(1);
            education.Verify(x => x.ShowRuleHelp(ruleId, filterableIssue.IssueId, null));
        }

        private static Mock<IErrorListHelper> CreateErrorListHelper(bool isSonarRule, SonarCompositeRuleId ruleId)
        {
            var mock = new Mock<IErrorListHelper>();
            mock.Setup(x => x.TryGetRuleId(It.IsAny<ITableEntryHandle>(), out ruleId)).Returns(isSonarRule);
            return mock;
        }

        private static SonarErrorListEventProcessor CreateTestSubject(IEducation educationService = null,
            IErrorListHelper errorListHelper = null,
            ILogger logger = null)
        {
            educationService ??= Mock.Of<IEducation>();
            errorListHelper ??= Mock.Of<IErrorListHelper>();
            logger ??= new TestLogger(logToConsole: true);

            var testSubject = new SonarErrorListEventProcessor(educationService, errorListHelper, logger);
            return testSubject;
        }
    }
}
