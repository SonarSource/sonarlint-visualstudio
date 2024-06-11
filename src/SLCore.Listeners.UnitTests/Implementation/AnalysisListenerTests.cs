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

using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Listener.Analysis;
using SonarLint.VisualStudio.SLCore.Listener.Analysis.Models;

namespace SonarLint.VisualStudio.SLCore.Listeners.UnitTests.Implementation;

[TestClass]
public class AnalysisListenerTests
{
    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<AnalysisListener, ISLCoreListener>(MefTestHelpers.CreateExport<IAnalysisService>(), MefTestHelpers.CreateExport<IRaiseIssueParamsToAnalysisIssueConverter>());
    }

    [TestMethod]
    public void MefCtor_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<AnalysisListener>();
    }

    [TestMethod]
    public void DidChangeAnalysisReadiness_DoesNothing()
    {
        var testSubject = CreateTestSubject();

        var result = testSubject.DidChangeAnalysisReadinessAsync(new DidChangeAnalysisReadinessParams(new List<string> { "id" }, true));

        result.Status.Should().Be(TaskStatus.RanToCompletion);
    }

    [TestMethod]
    public void RaiseIssues_AnalysisIDisNull_Ignores()
    {
        var raiseIssuesParams = new RaiseIssuesParams("CONFIGURATION_ID", new Dictionary<Uri, List<RaisedIssueDto>>(), false, null);

        var analysisService = Substitute.For<IAnalysisService>();
        var raiseIssueParamsToAnalysisIssueConverter = Substitute.For<IRaiseIssueParamsToAnalysisIssueConverter>();

        var testSubject = CreateTestSubject(analysisService, raiseIssueParamsToAnalysisIssueConverter);

        testSubject.RaiseIssues(raiseIssuesParams);

        analysisService.DidNotReceive().PublishIssues(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<IEnumerable<IAnalysisIssue>>());
        raiseIssueParamsToAnalysisIssueConverter.DidNotReceive().GetAnalysisIssues(Arg.Any<RaiseIssuesParams>());
    }

    [TestMethod]
    public void RaiseIssues_NoIssues_Ignores()
    {
        var issuesByFileUri = new Dictionary<Uri, List<RaisedIssueDto>> { { new Uri("file://C://sometfile"), new List<RaisedIssueDto>() } };

        var raiseIssuesParams = new RaiseIssuesParams("CONFIGURATION_ID", issuesByFileUri, false, Guid.NewGuid());

        var analysisService = Substitute.For<IAnalysisService>();
        var raiseIssueParamsToAnalysisIssueConverter = CreateConverter(raiseIssuesParams, Array.Empty<IAnalysisIssue>());

        var testSubject = CreateTestSubject(analysisService, raiseIssueParamsToAnalysisIssueConverter);

        testSubject.RaiseIssues(raiseIssuesParams);

        analysisService.DidNotReceive().PublishIssues(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<IEnumerable<IAnalysisIssue>>());
        raiseIssueParamsToAnalysisIssueConverter.Received(1).GetAnalysisIssues(raiseIssuesParams);
    }

    [TestMethod]
    public void RaiseIssues_NoSecrets_Ignores()
    {
        var issue1 = CreateIssue("csharpsquid:S100");
        var issue2 = CreateIssue("csharpsquid:S101");
        var issues = new[] { issue1, issue2 };

        var issuesByFileUri = new Dictionary<Uri, List<RaisedIssueDto>> { { new Uri("file://C://somefile"), new List<RaisedIssueDto>() } };

        var raiseIssuesParams = new RaiseIssuesParams("CONFIGURATION_ID", issuesByFileUri, false, Guid.NewGuid());

        var analysisService = Substitute.For<IAnalysisService>();
        IRaiseIssueParamsToAnalysisIssueConverter raiseIssueParamsToAnalysisIssueConverter = CreateConverter(raiseIssuesParams, issues);

        var testSubject = CreateTestSubject(analysisService, raiseIssueParamsToAnalysisIssueConverter);

        testSubject.RaiseIssues(raiseIssuesParams);

        analysisService.DidNotReceive().PublishIssues(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<IEnumerable<IAnalysisIssue>>());
        raiseIssueParamsToAnalysisIssueConverter.Received(1).GetAnalysisIssues(raiseIssuesParams);
    }

    [TestMethod]
    public void RaiseIssues_HasIssues_PublishIssues()
    {
        var guid = Guid.NewGuid();
        var issue1 = CreateIssue("csharpsquid:S100");
        var issue2 = CreateIssue("secrets:S101");
        var issue3 = CreateIssue("secrets:S1012");
        var issues = new[] { issue1, issue2, issue3 };

        var issuesByFileUri = new Dictionary<Uri, List<RaisedIssueDto>> { { new Uri("file://C://somefile"), new List<RaisedIssueDto>() } };

        var raiseIssuesParams = new RaiseIssuesParams("CONFIGURATION_ID", issuesByFileUri, false, guid);

        var analysisService = Substitute.For<IAnalysisService>();
        IRaiseIssueParamsToAnalysisIssueConverter raiseIssueParamsToAnalysisIssueConverter = CreateConverter(raiseIssuesParams, issues);

        var testSubject = CreateTestSubject(analysisService, raiseIssueParamsToAnalysisIssueConverter);

        testSubject.RaiseIssues(raiseIssuesParams);

        analysisService.Received(1).PublishIssues("C:\\\\somefile", guid, Arg.Is<IEnumerable<IAnalysisIssue>>(
           publishedIssues => publishedIssues.Count() == 2 && publishedIssues.ElementAt(0) == issue2 && publishedIssues.ElementAt(1) == issue3));
        raiseIssueParamsToAnalysisIssueConverter.Received(1).GetAnalysisIssues(raiseIssuesParams);
    }

    private static IRaiseIssueParamsToAnalysisIssueConverter CreateConverter(RaiseIssuesParams raiseIssuesParams, IAnalysisIssue[] issues)
    {
        var raiseIssueParamsToAnalysisIssueConverter = Substitute.For<IRaiseIssueParamsToAnalysisIssueConverter>();
        raiseIssueParamsToAnalysisIssueConverter.GetAnalysisIssues(raiseIssuesParams).Returns(issues);
        return raiseIssueParamsToAnalysisIssueConverter;
    }

    [TestMethod]
    public void RaiseHotspots_ThrowsNotImplemented()
    {
        {
            var raiseHotspotsParams = new RaiseHotspotsParams("CONFIGURATION_ID", new Dictionary<Uri, List<RaisedHotspotDto>>(), false, Guid.NewGuid());

            var testSubject = CreateTestSubject();

            var act = () => testSubject.RaiseHotspots(raiseHotspotsParams);

            act.Should().Throw<NotImplementedException>();
        }
    }

    private AnalysisListener CreateTestSubject(IAnalysisService analysisService = null, IRaiseIssueParamsToAnalysisIssueConverter raiseIssueParamsToAnalysisIssueConverter = null)
        => new AnalysisListener(analysisService ?? Substitute.For<IAnalysisService>(), raiseIssueParamsToAnalysisIssueConverter ?? Substitute.For<IRaiseIssueParamsToAnalysisIssueConverter>());

    private static IAnalysisIssue CreateIssue(string ruleKey)
    {
        var issue1 = Substitute.For<IAnalysisIssue>();
        issue1.RuleKey.Returns(ruleKey);
        return issue1;
    }
}
