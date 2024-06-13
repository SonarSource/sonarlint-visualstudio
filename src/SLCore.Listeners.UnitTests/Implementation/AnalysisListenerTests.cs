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
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Listener.Analysis;
using SonarLint.VisualStudio.SLCore.Listener.Analysis.Models;
using Language = SonarLint.VisualStudio.Core.Language;

namespace SonarLint.VisualStudio.SLCore.Listeners.UnitTests.Implementation;

[TestClass]
public class AnalysisListenerTests
{
    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<AnalysisListener, ISLCoreListener>
            (MefTestHelpers.CreateExport<IAnalysisService>()
            , MefTestHelpers.CreateExport<IRaiseIssueParamsToAnalysisIssueConverter>()
            , MefTestHelpers.CreateExport<IAnalysisStatusNotifierFactory>());
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
        var raiseIssuesParams = new RaiseIssuesParams("CONFIGURATION_ID", new Dictionary<FileUri, List<RaisedIssueDto>>(), false, null);

        var analysisService = Substitute.For<IAnalysisService>();
        var raiseIssueParamsToAnalysisIssueConverter = Substitute.For<IRaiseIssueParamsToAnalysisIssueConverter>();

        var testSubject = CreateTestSubject(analysisService, raiseIssueParamsToAnalysisIssueConverter);

        testSubject.RaiseIssues(raiseIssuesParams);

        analysisService.DidNotReceive().PublishIssues(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<IEnumerable<IAnalysisIssue>>());
        raiseIssueParamsToAnalysisIssueConverter.DidNotReceive().GetAnalysisIssues(Arg.Any<FileUri>(), Arg.Any<List<RaisedIssueDto>>());
    }

    [TestMethod]
    public void RaiseIssues_NoIssues_Ignores()
    {
        var issuesByFileUri = new Dictionary<FileUri, List<RaisedIssueDto>> { { new FileUri("file://C:/somefile"), new List<RaisedIssueDto>() } };

        var raiseIssuesParams = new RaiseIssuesParams("CONFIGURATION_ID", issuesByFileUri, false, Guid.NewGuid());

        var analysisService = Substitute.For<IAnalysisService>();
        var raiseIssueParamsToAnalysisIssueConverter = CreateConverter(issuesByFileUri.Single().Key, Array.Empty<IAnalysisIssue>());

        var testSubject = CreateTestSubject(analysisService, raiseIssueParamsToAnalysisIssueConverter);

        testSubject.RaiseIssues(raiseIssuesParams);

        analysisService.DidNotReceive().PublishIssues(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<IEnumerable<IAnalysisIssue>>());
        raiseIssueParamsToAnalysisIssueConverter.DidNotReceive().GetAnalysisIssues(Arg.Any<FileUri>(), Arg.Any<List<RaisedIssueDto>>());
    }

    [TestMethod]
    public void RaiseIssues_NoSupportedLanguages_Ignores()
    {
        var issue1 = CreateIssue("csharpsquid:S100");
        var issue2 = CreateIssue("csharpsquid:S101");
        var issues = new[] { issue1, issue2 };

        var issuesByFileUri = new Dictionary<FileUri, List<RaisedIssueDto>> { { new FileUri("file://C:/somefile"), new List<RaisedIssueDto>() } };

        var raiseIssuesParams = new RaiseIssuesParams("CONFIGURATION_ID", issuesByFileUri, false, Guid.NewGuid());

        var analysisService = Substitute.For<IAnalysisService>();
        IRaiseIssueParamsToAnalysisIssueConverter raiseIssueParamsToAnalysisIssueConverter = CreateConverter(issuesByFileUri.Single().Key, issues);

        var testSubject = CreateTestSubject(analysisService, raiseIssueParamsToAnalysisIssueConverter);

        testSubject.RaiseIssues(raiseIssuesParams);

        analysisService.DidNotReceive().PublishIssues(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<IEnumerable<IAnalysisIssue>>());
        raiseIssueParamsToAnalysisIssueConverter.DidNotReceive().GetAnalysisIssues(Arg.Any<FileUri>(), Arg.Any<List<RaisedIssueDto>>());
    }

    [TestMethod]
    public void RaiseIssues_HasMoreThanOneFileUri_Throws()
    {
        var issuesByFileUri = new Dictionary<FileUri, List<RaisedIssueDto>> { { new FileUri("file://C:/somefile"), new List<RaisedIssueDto>() }, { new FileUri("file://C:/someOtherfile"), new List<RaisedIssueDto>() } };
        var testSubject = CreateTestSubject();

        var act = () => testSubject.RaiseIssues(new RaiseIssuesParams("CONFIGURATION_ID", issuesByFileUri, false, Guid.NewGuid()));

        act.Should().Throw<InvalidOperationException>();
    }

    [TestMethod]
    public void RaiseIssues_HasNoFileUri_Throws()
    {
        var testSubject = CreateTestSubject();

        var act = () => testSubject.RaiseIssues(new RaiseIssuesParams("CONFIGURATION_ID", new Dictionary<FileUri, List<RaisedIssueDto>>(), false, Guid.NewGuid()));

        act.Should().Throw<InvalidOperationException>();
    }

    [DataRow(true)]
    [DataRow(false)]
    [TestMethod]
    public void RaiseIssues_HasIssues_PublishIssues(bool isIntermediatePublication)
    {
        var guid = Guid.NewGuid();
        var issue1 = CreateIssue("csharpsquid:S100");
        var issue2 = CreateIssue("secrets:S1012");
        var filteredIssues = new[] { issue1, issue2 };

        var raisedIssue1 = CreateRaisedIssueDto("csharpsquid:S100");
        var raisedIssue2 = CreateRaisedIssueDto("javascript:S101");
        var raisedIssue3 = CreateRaisedIssueDto("secrets:S1012");
        var raisedIssues = new List<RaisedIssueDto> { raisedIssue1, raisedIssue2, raisedIssue3 };

        var issuesByFileUri = new Dictionary<FileUri, List<RaisedIssueDto>> { { new FileUri("file://C:/somefile"), raisedIssues } };

        var raiseIssuesParams = new RaiseIssuesParams("CONFIGURATION_ID", issuesByFileUri, isIntermediatePublication, guid);

        var analysisService = Substitute.For<IAnalysisService>();
        IRaiseIssueParamsToAnalysisIssueConverter raiseIssueParamsToAnalysisIssueConverter = CreateConverter(issuesByFileUri.Single().Key, filteredIssues);

        var analysisStatusNotifierFactory = CreateAnalysisStatusNotifierFactory(out var analysisStatusNotifier);

        var testSubject = CreateTestSubject(analysisService, raiseIssueParamsToAnalysisIssueConverter, analysisStatusNotifierFactory, new List<Language> { Language.Secrets, Language.CSharp });

        testSubject.RaiseIssues(raiseIssuesParams);

        analysisService.Received(1).PublishIssues("C:\\somefile", guid, filteredIssues);
        raiseIssueParamsToAnalysisIssueConverter.Received(1).GetAnalysisIssues(issuesByFileUri.Single().Key, Arg.Is<IEnumerable<RaisedIssueDto>>(
            filteredIssues => filteredIssues.Count() == 2 && filteredIssues.ElementAt(0) == raisedIssue1 && filteredIssues.ElementAt(1) == raisedIssue3));

        if (isIntermediatePublication)
        {
            analysisStatusNotifierFactory.DidNotReceive().Create(Arg.Any<string>(), Arg.Any<string>());
        }
        else
        {
            analysisStatusNotifierFactory.Received(1).Create("SLCoreAnalyzer", "C:\\somefile");
            analysisStatusNotifier.Received(1).AnalysisFinished(2, TimeSpan.Zero);
        }
    }

    private static IRaiseIssueParamsToAnalysisIssueConverter CreateConverter(FileUri fileUri, IAnalysisIssue[] issues)
    {
        var raiseIssueParamsToAnalysisIssueConverter = Substitute.For<IRaiseIssueParamsToAnalysisIssueConverter>();
        raiseIssueParamsToAnalysisIssueConverter.GetAnalysisIssues(fileUri, Arg.Any<IEnumerable<RaisedIssueDto>>()).Returns(issues);
        return raiseIssueParamsToAnalysisIssueConverter;
    }

    private AnalysisListener CreateTestSubject(IAnalysisService analysisService = null,
        IRaiseIssueParamsToAnalysisIssueConverter raiseIssueParamsToAnalysisIssueConverter = null,
        IAnalysisStatusNotifierFactory analysisStatusNotifierFactory = null,
        IEnumerable<Language> languages = null)
        => new AnalysisListener(analysisService ?? Substitute.For<IAnalysisService>(),
            raiseIssueParamsToAnalysisIssueConverter ?? Substitute.For<IRaiseIssueParamsToAnalysisIssueConverter>(),
            analysisStatusNotifierFactory ?? CreateAnalysisStatusNotifierFactory(out _),
            languages ?? new List<Language>() { Language.Secrets });

    private IAnalysisStatusNotifierFactory CreateAnalysisStatusNotifierFactory(out IAnalysisStatusNotifier analysisStatusNotifier)
    {
        var analysisStatusNotifierFactory = Substitute.For<IAnalysisStatusNotifierFactory>();
        analysisStatusNotifier = Substitute.For<IAnalysisStatusNotifier>();
        analysisStatusNotifierFactory.Create(Arg.Any<string>(), Arg.Any<string>()).Returns(analysisStatusNotifier);
        return analysisStatusNotifierFactory;
    }

    private RaisedIssueDto CreateRaisedIssueDto(string ruleKey)
    {
        return new RaisedIssueDto(default, default, ruleKey, default, default, default, default, default, default, default, default, default, default, default, default);
    }

    private static IAnalysisIssue CreateIssue(string ruleKey)
    {
        var issue1 = Substitute.For<IAnalysisIssue>();
        issue1.RuleKey.Returns(ruleKey);
        return issue1;
    }
}
