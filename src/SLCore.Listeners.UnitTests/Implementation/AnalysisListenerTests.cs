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
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.SLCore.Analysis;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Configuration;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Listener.Analysis;
using SonarLint.VisualStudio.SLCore.Listener.Analysis.Models;
using SonarLint.VisualStudio.SLCore.State;
using SloopLanguage = SonarLint.VisualStudio.SLCore.Common.Models.Language;

namespace SonarLint.VisualStudio.SLCore.Listeners.UnitTests.Implementation;

[TestClass]
public class AnalysisListenerTests
{
    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<AnalysisListener, ISLCoreListener>(
            MefTestHelpers.CreateExport<ISLCoreConstantsProvider>(),
            MefTestHelpers.CreateExport<IActiveConfigScopeTracker>(),
            MefTestHelpers.CreateExport<IAnalysisRequester>(),
            MefTestHelpers.CreateExport<IAnalysisService>(),
            MefTestHelpers.CreateExport<IRaiseIssueParamsToAnalysisIssueConverter>(),
            MefTestHelpers.CreateExport<IAnalysisStatusNotifierFactory>(),
            MefTestHelpers.CreateExport<ILogger>());
    }

    [TestMethod]
    public void MefCtor_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<AnalysisListener>();
    }

    [TestMethod]
    public void DidChangeAnalysisReadiness_NotSuccessful_LogsConfigScopeConflict()
    {
        var activeConfigScopeTracker = Substitute.For<IActiveConfigScopeTracker>();
        activeConfigScopeTracker.TryUpdateAnalysisReadinessOnCurrentConfigScope(Arg.Any<string>(), Arg.Any<bool>()).Returns(false);
        var testLogger = new TestLogger();
        var testSubject = CreateTestSubject(activeConfigScopeTracker, logger: testLogger);

        testSubject.DidChangeAnalysisReadiness(new DidChangeAnalysisReadinessParams(new List<string> { "id" }, true));

        activeConfigScopeTracker.Received().TryUpdateAnalysisReadinessOnCurrentConfigScope("id", true);
        testLogger.AssertPartialOutputStringExists(string.Format(SLCoreStrings.AnalysisReadinessUpdate, SLCoreStrings.ConfigScopeConflict));
    }

    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void DidChangeAnalysisReadiness_Successful_LogsState(bool isReady)
    {
        var activeConfigScopeTracker = Substitute.For<IActiveConfigScopeTracker>();
        activeConfigScopeTracker.TryUpdateAnalysisReadinessOnCurrentConfigScope(Arg.Any<string>(), Arg.Any<bool>()).Returns(true);
        var analysisRequester = Substitute.For<IAnalysisRequester>();
        var testLogger = new TestLogger();
        var testSubject = CreateTestSubject(activeConfigScopeTracker, analysisRequester: analysisRequester, logger: testLogger);

        testSubject.DidChangeAnalysisReadiness(new DidChangeAnalysisReadinessParams(new List<string> { "id" }, isReady));

        activeConfigScopeTracker.Received().TryUpdateAnalysisReadinessOnCurrentConfigScope("id", isReady);
        if (isReady)
        {
            analysisRequester.Received().RequestAnalysis(Arg.Is<IAnalyzerOptions>(o => o.IsOnOpen), Arg.Is<string[]>(s => !s.Any()));
        }

        testLogger.AssertPartialOutputStringExists(string.Format(SLCoreStrings.AnalysisReadinessUpdate, isReady));
    }

    [TestMethod]
    public void RaiseIssues_AnalysisIDisNull_Ignores()
    {
        var raiseIssuesParams = new RaiseIssuesParams("CONFIGURATION_ID", new Dictionary<FileUri, List<RaisedIssueDto>>(), false, null);

        var analysisService = Substitute.For<IAnalysisService>();
        var raiseIssueParamsToAnalysisIssueConverter = Substitute.For<IRaiseIssueParamsToAnalysisIssueConverter>();

        var testSubject = CreateTestSubject(analysisService: analysisService,
            raiseIssueParamsToAnalysisIssueConverter: raiseIssueParamsToAnalysisIssueConverter);

        testSubject.RaiseIssues(raiseIssuesParams);

        analysisService.DidNotReceive().PublishIssues(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<IEnumerable<IAnalysisIssue>>());
        raiseIssueParamsToAnalysisIssueConverter.DidNotReceive().GetAnalysisIssues(Arg.Any<FileUri>(), Arg.Any<List<RaisedIssueDto>>());
    }

    [TestMethod]
    public void RaiseIssues_NoIssues_Ignores()
    {
        var fileUri = new FileUri("file://C:/somefile");
        var analysisId = Guid.NewGuid();
        var issuesByFileUri = new Dictionary<FileUri, List<RaisedIssueDto>> { { fileUri, [] } };

        var raiseIssuesParams = new RaiseIssuesParams("CONFIGURATION_ID", issuesByFileUri, false, analysisId);

        var analysisService = Substitute.For<IAnalysisService>();
        var raiseIssueParamsToAnalysisIssueConverter = CreateConverter(issuesByFileUri.Single().Key, [], []);

        var testSubject = CreateTestSubject(analysisService: analysisService,
            raiseIssueParamsToAnalysisIssueConverter: raiseIssueParamsToAnalysisIssueConverter);

        testSubject.RaiseIssues(raiseIssuesParams);

        raiseIssueParamsToAnalysisIssueConverter.Received().GetAnalysisIssues(fileUri, Arg.Is<IEnumerable<RaisedIssueDto>>(x => !x.Any()));
        analysisService.Received().PublishIssues(fileUri.LocalPath, analysisId, Arg.Is<IEnumerable<IAnalysisIssue>>(x => !x.Any()));
    }

    [TestMethod]
    public void RaiseIssues_NoSupportedLanguages_PublishesEmpty()
    {
        var issue1 = CreateIssue("csharpsquid:S100");
        var issue2 = CreateIssue("csharpsquid:S101");
        var issues = new[] { issue1, issue2 };

        var analysisId = Guid.NewGuid();
        var fileUri = new FileUri("file://C:/somefile");
        var issuesByFileUri = new Dictionary<FileUri, List<RaisedIssueDto>>
            { { fileUri, [CreateRaisedIssueDto("csharpsquid:S100"), CreateRaisedIssueDto("csharpsquid:S101")] } };

        var raiseIssuesParams = new RaiseIssuesParams("CONFIGURATION_ID", issuesByFileUri, false, analysisId);

        var analysisService = Substitute.For<IAnalysisService>();
        IRaiseIssueParamsToAnalysisIssueConverter raiseIssueParamsToAnalysisIssueConverter = CreateConverter(issuesByFileUri.Single().Key, [], []);

        var analysisStatusNotifierFactory = CreateAnalysisStatusNotifierFactory(out var analysisStatusNotifier, fileUri.LocalPath, analysisId);

        var testSubject = CreateTestSubject(analysisService: analysisService,
            raiseIssueParamsToAnalysisIssueConverter: raiseIssueParamsToAnalysisIssueConverter,
            analysisStatusNotifierFactory: analysisStatusNotifierFactory,
            slCoreConstantsProvider:CreateConstantsProviderWithLanguages([]));

        testSubject.RaiseIssues(raiseIssuesParams);

        raiseIssueParamsToAnalysisIssueConverter.Received().GetAnalysisIssues(fileUri, Arg.Is<IEnumerable<RaisedIssueDto>>(x => !x.Any()));
        analysisService.Received().PublishIssues(fileUri.LocalPath, analysisId, Arg.Is<IEnumerable<IAnalysisIssue>>(x => !x.Any()));
        analysisStatusNotifier.AnalysisFinished(0, TimeSpan.Zero);
    }
    
    [TestMethod]
    public void RaiseIssues_NoKnownLanguages_PublishesEmpty()
    {
        var issue1 = CreateIssue("csharpsquid:S100");
        var issue2 = CreateIssue("csharpsquid:S101");
        var issues = new[] { issue1, issue2 };

        var analysisId = Guid.NewGuid();
        var fileUri = new FileUri("file://C:/somefile");
        var issuesByFileUri = new Dictionary<FileUri, List<RaisedIssueDto>>
            { { fileUri, [CreateRaisedIssueDto("csharpsquid:S100"), CreateRaisedIssueDto("csharpsquid:S101")] } };

        var raiseIssuesParams = new RaiseIssuesParams("CONFIGURATION_ID", issuesByFileUri, false, analysisId);

        var analysisService = Substitute.For<IAnalysisService>();
        IRaiseIssueParamsToAnalysisIssueConverter raiseIssueParamsToAnalysisIssueConverter = CreateConverter(issuesByFileUri.Single().Key, [], []);

        var analysisStatusNotifierFactory = CreateAnalysisStatusNotifierFactory(out var analysisStatusNotifier, fileUri.LocalPath, analysisId);

        var testSubject = CreateTestSubject(analysisService: analysisService,
            raiseIssueParamsToAnalysisIssueConverter: raiseIssueParamsToAnalysisIssueConverter,
            analysisStatusNotifierFactory: analysisStatusNotifierFactory,
            slCoreConstantsProvider:CreateConstantsProviderWithLanguages([SloopLanguage.JAVA]));

        testSubject.RaiseIssues(raiseIssuesParams);

        raiseIssueParamsToAnalysisIssueConverter.Received().GetAnalysisIssues(fileUri, Arg.Is<IEnumerable<RaisedIssueDto>>(x => !x.Any()));
        analysisService.Received().PublishIssues(fileUri.LocalPath, analysisId, Arg.Is<IEnumerable<IAnalysisIssue>>(x => !x.Any()));
        analysisStatusNotifier.AnalysisFinished(0, TimeSpan.Zero);
    }

    [TestMethod]
    public void RaiseIssues_HasNoFileUri_FinishesAnalysis()
    {
        var analysisId = Guid.NewGuid();
        var analysisStatusNotifierFactory = CreateAnalysisStatusNotifierFactory(out var analysisStatusNotifier, null, analysisId);
        var analysisService = Substitute.For<IAnalysisService>();
        var testSubject = CreateTestSubject(analysisService: analysisService, analysisStatusNotifierFactory: analysisStatusNotifierFactory);

        var act = () =>
            testSubject.RaiseIssues(new RaiseIssuesParams("CONFIGURATION_ID", new Dictionary<FileUri, List<RaisedIssueDto>>(), false, analysisId));

        act.Should().NotThrow();
        analysisService.ReceivedCalls().Should().BeEmpty();
        analysisStatusNotifier.ReceivedCalls().Should().BeEmpty();
    }

    [TestMethod]
    public void RaiseIssues_HasIssuesNotIntermediate_PublishIssues()
    {
        var analysisId = Guid.NewGuid();
        var fileUri = new FileUri("file://C:/somefile");
        var issue1 = CreateIssue("csharpsquid:S100");
        var issue2 = CreateIssue("secrets:S1012");
        var filteredIssues = new[] { issue1, issue2 };

        var raisedIssue1 = CreateRaisedIssueDto("csharpsquid:S100");
        var raisedIssue2 = CreateRaisedIssueDto("javascript:S101");
        var raisedIssue3 = CreateRaisedIssueDto("secrets:S1012");
        var filteredRaisedIssues = new[] { raisedIssue1, raisedIssue3 };
        var raisedIssues = new List<RaisedIssueDto> { raisedIssue1, raisedIssue2, raisedIssue3 };

        var issuesByFileUri = new Dictionary<FileUri, List<RaisedIssueDto>> { { fileUri, raisedIssues } };

        var raiseIssuesParams = new RaiseIssuesParams("CONFIGURATION_ID", issuesByFileUri, false, analysisId);

        var analysisService = Substitute.For<IAnalysisService>();
        IRaiseIssueParamsToAnalysisIssueConverter raiseIssueParamsToAnalysisIssueConverter =
            CreateConverter(issuesByFileUri.Single().Key, filteredRaisedIssues, filteredIssues);

        var analysisStatusNotifierFactory = CreateAnalysisStatusNotifierFactory(out var analysisStatusNotifier, fileUri.LocalPath, analysisId);

        var testSubject = CreateTestSubject(analysisService: analysisService,
            raiseIssueParamsToAnalysisIssueConverter: raiseIssueParamsToAnalysisIssueConverter,
            analysisStatusNotifierFactory: analysisStatusNotifierFactory,
            slCoreConstantsProvider: CreateConstantsProviderWithLanguages(SloopLanguage.SECRETS, SloopLanguage.CS));

        testSubject.RaiseIssues(raiseIssuesParams);

        analysisService.Received(1).PublishIssues(fileUri.LocalPath, analysisId, filteredIssues);
        raiseIssueParamsToAnalysisIssueConverter.Received(1).GetAnalysisIssues(issuesByFileUri.Single().Key, Arg.Is<IEnumerable<RaisedIssueDto>>(
            x => x.SequenceEqual(filteredRaisedIssues)));

        analysisStatusNotifierFactory.Received(1).Create("SLCoreAnalyzer", fileUri.LocalPath, analysisId);
        analysisStatusNotifier.Received(1).AnalysisFinished(2, TimeSpan.Zero);
    }

    [TestMethod]
    public void RaiseIssues_MultipleFiles_PublishIssuesForEachFile()
    {
        var analysisId = Guid.NewGuid();
        var fileUri1 = new FileUri("file://C:/somefile");
        var fileUri2 = new FileUri("file://C:/someOtherfile");
        var issue1 = CreateIssue("secrets:S100");
        var issue2 = CreateIssue("secrets:S101");

        var raisedIssue1 = CreateRaisedIssueDto("secrets:S100");
        var raisedIssue2 = CreateRaisedIssueDto("secrets:S101");

        var issuesByFileUri = new Dictionary<FileUri, List<RaisedIssueDto>> { { fileUri1, [raisedIssue1] }, { fileUri2, [raisedIssue2] } };

        var raiseIssuesParams = new RaiseIssuesParams("CONFIGURATION_ID", issuesByFileUri, false, analysisId);

        var analysisService = Substitute.For<IAnalysisService>();
        var raiseIssueParamsToAnalysisIssueConverter = Substitute.For<IRaiseIssueParamsToAnalysisIssueConverter>();
        raiseIssueParamsToAnalysisIssueConverter.GetAnalysisIssues(fileUri1, Arg.Any<IEnumerable<RaisedIssueDto>>()).Returns([issue1]);
        raiseIssueParamsToAnalysisIssueConverter.GetAnalysisIssues(fileUri2, Arg.Any<IEnumerable<RaisedIssueDto>>()).Returns([issue2]);

        var analysisStatusNotifierFactory = Substitute.For<IAnalysisStatusNotifierFactory>();

        var testSubject = CreateTestSubject(analysisService: analysisService,
            raiseIssueParamsToAnalysisIssueConverter: raiseIssueParamsToAnalysisIssueConverter,
            analysisStatusNotifierFactory: analysisStatusNotifierFactory);

        testSubject.RaiseIssues(raiseIssuesParams);

        analysisService.Received(1).PublishIssues(fileUri1.LocalPath, analysisId,
            Arg.Is<IEnumerable<IAnalysisIssue>>(x => x.SequenceEqual(new List<IAnalysisIssue> { issue1 })));
        analysisService.Received(1).PublishIssues(fileUri2.LocalPath, analysisId,
            Arg.Is<IEnumerable<IAnalysisIssue>>(x => x.SequenceEqual(new List<IAnalysisIssue> { issue2 })));

        analysisStatusNotifierFactory.Received(1).Create("SLCoreAnalyzer", fileUri1.LocalPath, analysisId);
        analysisStatusNotifierFactory.Received(1).Create("SLCoreAnalyzer", fileUri2.LocalPath, analysisId);
    }

    [TestMethod]
    public void RaiseIssues_HasIssuesIntermediate_DoNotPublishIssues()
    {
        var raisedIssue1 = CreateRaisedIssueDto("csharpsquid:S100");
        var raisedIssue2 = CreateRaisedIssueDto("javascript:S101");
        var raisedIssue3 = CreateRaisedIssueDto("secrets:S1012");
        var raisedIssues = new List<RaisedIssueDto> { raisedIssue1, raisedIssue2, raisedIssue3 };

        var issuesByFileUri = new Dictionary<FileUri, List<RaisedIssueDto>> { { new FileUri("file://C:/somefile"), raisedIssues } };
        var raiseIssuesParams = new RaiseIssuesParams("CONFIGURATION_ID", issuesByFileUri, true, Guid.NewGuid());

        var analysisService = Substitute.For<IAnalysisService>();

        var testSubject = CreateTestSubject(analysisService: analysisService);

        testSubject.RaiseIssues(raiseIssuesParams);

        analysisService.DidNotReceiveWithAnyArgs().PublishIssues(default, default, default);
    }

    private static IRaiseIssueParamsToAnalysisIssueConverter CreateConverter(FileUri fileUri, IReadOnlyCollection<RaisedIssueDto> raisedIssueDtos,
        IAnalysisIssue[] issues)
    {
        var raiseIssueParamsToAnalysisIssueConverter = Substitute.For<IRaiseIssueParamsToAnalysisIssueConverter>();
        raiseIssueParamsToAnalysisIssueConverter
            .GetAnalysisIssues(fileUri, Arg.Is<IEnumerable<RaisedIssueDto>>(x => x.SequenceEqual(raisedIssueDtos))).Returns(issues);
        return raiseIssueParamsToAnalysisIssueConverter;
    }

    private AnalysisListener CreateTestSubject(IActiveConfigScopeTracker activeConfigScopeTracker = null,
        IAnalysisService analysisService = null,
        IAnalysisRequester analysisRequester = null,
        IRaiseIssueParamsToAnalysisIssueConverter raiseIssueParamsToAnalysisIssueConverter = null,
        IAnalysisStatusNotifierFactory analysisStatusNotifierFactory = null,
        ILogger logger = null,
        ISLCoreConstantsProvider slCoreConstantsProvider = null)
        => new(slCoreConstantsProvider ?? CreateConstantsProviderWithLanguages(SloopLanguage.SECRETS), activeConfigScopeTracker ?? Substitute.For<IActiveConfigScopeTracker>(),
            analysisRequester ?? Substitute.For<IAnalysisRequester>(), analysisService ?? Substitute.For<IAnalysisService>(),
            raiseIssueParamsToAnalysisIssueConverter ?? Substitute.For<IRaiseIssueParamsToAnalysisIssueConverter>(),
            analysisStatusNotifierFactory ?? Substitute.For<IAnalysisStatusNotifierFactory>(), logger ?? new TestLogger());

    private ISLCoreConstantsProvider CreateConstantsProviderWithLanguages(params SloopLanguage[] languages)
    {
        var slCoreConstantsProvider = Substitute.For<ISLCoreConstantsProvider>();
        slCoreConstantsProvider.SLCoreAnalyzableLanguages.Returns(languages.ToList());
        return slCoreConstantsProvider;
    }

private IAnalysisStatusNotifierFactory CreateAnalysisStatusNotifierFactory(out IAnalysisStatusNotifier analysisStatusNotifier, string filePath,
        Guid? analysisId)
    {
        var analysisStatusNotifierFactory = Substitute.For<IAnalysisStatusNotifierFactory>();
        analysisStatusNotifier = Substitute.For<IAnalysisStatusNotifier>();
        analysisStatusNotifierFactory.Create(nameof(SLCoreAnalyzer), filePath, analysisId).Returns(analysisStatusNotifier);
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
