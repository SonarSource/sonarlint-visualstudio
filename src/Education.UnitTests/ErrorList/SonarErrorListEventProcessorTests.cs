/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Suppressions;
using SonarLint.VisualStudio.Education.ErrorList;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Education.UnitTests.ErrorList;

[TestClass]
public class SonarErrorListEventProcessorTests
{
    private readonly TableEntryEventArgs eventArgs = new();

    private IEducation education;
    private IErrorListHelper errorListHelper;
    private IFilterableIssue filterableIssue;
    private ITableEntryHandle handle;
    private ILogger logger;
    private SonarErrorListEventProcessor testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        education = Substitute.For<IEducation>();
        errorListHelper = Substitute.For<IErrorListHelper>();
        handle = Substitute.For<ITableEntryHandle>();
        filterableIssue = Substitute.For<IFilterableIssue>();
        logger = new TestLogger();

        testSubject = new SonarErrorListEventProcessor(education, errorListHelper, logger);
    }

    [TestMethod]
    public void PreprocessNavigateToHelp_NotASonarRule_EventIsNotHandled()
    {
        SonarCompositeRuleId ruleId = null;
        MockErrorListHelper(false, ruleId);

        testSubject.PreprocessNavigateToHelp(handle, eventArgs);

        errorListHelper.Received(1).TryGetRuleId(handle, out _);
        education.ReceivedCalls().Should().HaveCount(0);
        eventArgs.Handled.Should().BeFalse();
    }

    [TestMethod]
    public void PreprocessNavigateToHelp_IsASonarRule_EventIsHandledAndEducationServiceCalled()
    {
        var ruleId = CreateSonarCompositeRuleId("cpp:S123");
        MockErrorListHelper(true, ruleId);

        testSubject.PreprocessNavigateToHelp(handle, eventArgs);

        errorListHelper.Received(1).TryGetRuleId(handle, out _);
        education.ReceivedCalls().Should().HaveCount(1);
        education.Received(1).ShowRuleHelp(ruleId, null);
        eventArgs.Handled.Should().BeTrue();
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void PreprocessNavigateToHelp_IsASonarRule_EducationServiceIsCalledWithIssueId(bool getFilterableIssueResult)
    {
        var ruleId = CreateSonarCompositeRuleId("cpp:S123");
        MockErrorListHelper(true, ruleId);
        MockGetFilterableIssue(getFilterableIssueResult);

        testSubject.PreprocessNavigateToHelp(handle, new TableEntryEventArgs());

        education.ReceivedCalls().Should().HaveCount(1);
        education.Received(1).ShowRuleHelp(ruleId, filterableIssue.IssueId);
    }

    private void MockGetFilterableIssue(bool getFilterableIssueResult) =>
        errorListHelper.TryGetFilterableIssue(Arg.Any<ITableEntryHandle>(), out _).Returns(callInfo =>
        {
            callInfo[1] = filterableIssue;
            return getFilterableIssueResult;
        });

    private void MockErrorListHelper(bool isSonarRule, SonarCompositeRuleId ruleId) =>
        errorListHelper.TryGetRuleId(Arg.Any<ITableEntryHandle>(), out _).Returns(callInfo =>
        {
            callInfo[1] = ruleId;
            return isSonarRule;
        });

    private static SonarCompositeRuleId CreateSonarCompositeRuleId(string errorListErrorCode)
    {
        SonarCompositeRuleId.TryParse(errorListErrorCode, out var ruleId);
        return ruleId;
    }
}
