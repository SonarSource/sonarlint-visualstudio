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
using SonarLint.VisualStudio.SLCore.Listener.Analysis;
using SonarLint.VisualStudio.SLCore.Listener.Analysis.Models;
using SonarLint.VisualStudio.SLCore.Listeners.Implementation.Analysis;
using CleanCodeAttribute = SonarLint.VisualStudio.SLCore.Common.Models.CleanCodeAttribute;
using IssueSeverity = SonarLint.VisualStudio.SLCore.Common.Models.IssueSeverity;
using SloopLanguage = SonarLint.VisualStudio.SLCore.Common.Models.Language;

namespace SonarLint.VisualStudio.SLCore.Listeners.UnitTests.Implementation.Analysis;

[TestClass]
public class RaisedFindingProcessorTests
{
    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<RaisedFindingProcessor, IRaisedFindingProcessor>(
            MefTestHelpers.CreateExport<ISLCoreConstantsProvider>(),
            MefTestHelpers.CreateExport<IAnalysisService>(),
            MefTestHelpers.CreateExport<IRaiseFindingToAnalysisIssueConverter>(),
            MefTestHelpers.CreateExport<IAnalysisStatusNotifierFactory>(),
            MefTestHelpers.CreateExport<ILogger>());
    }

    [TestMethod]
    public void MefCtor_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<RaisedFindingProcessor>();
    }
    
    [TestMethod]
    public void RaiseFindings_AnalysisIDisNull_Ignores()
    {
        var raiseFindingParams = new RaiseFindingParams<TestFinding>("CONFIGURATION_ID", new Dictionary<FileUri, List<TestFinding>>(), false, null);

        var analysisService = Substitute.For<IAnalysisService>();
        var raiseFindingParamsToAnalysisIssueConverter = Substitute.For<IRaiseFindingToAnalysisIssueConverter>();

        var testSubject = CreateTestSubject(analysisService: analysisService,
            raiseFindingToAnalysisIssueConverter: raiseFindingParamsToAnalysisIssueConverter);

        testSubject.RaiseFinding(raiseFindingParams);

        analysisService.DidNotReceive().PublishIssues(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<IEnumerable<IAnalysisIssue>>());
        raiseFindingParamsToAnalysisIssueConverter.DidNotReceive().GetAnalysisIssues(Arg.Any<FileUri>(), Arg.Any<List<TestFinding>>());
    }
    
    [TestMethod]
    public void RaiseFindings_NoFindings_Ignores()
    {
        var fileUri = new FileUri("file://C:/somefile");
        var analysisId = Guid.NewGuid();
        var findingsByFileUri = new Dictionary<FileUri, List<TestFinding>> { { fileUri, [] } };

        var raiseFindingParams = new RaiseFindingParams<TestFinding>("CONFIGURATION_ID", findingsByFileUri, false, analysisId);

        var analysisService = Substitute.For<IAnalysisService>();
        var raiseFindingParamsToAnalysisIssueConverter = CreateConverter(findingsByFileUri.Single().Key, [], []);

        var testSubject = CreateTestSubject(analysisService: analysisService,
            raiseFindingToAnalysisIssueConverter: raiseFindingParamsToAnalysisIssueConverter);

        testSubject.RaiseFinding(raiseFindingParams);

        raiseFindingParamsToAnalysisIssueConverter.Received().GetAnalysisIssues(fileUri, Arg.Is<IEnumerable<RaisedFindingDto>>(x => !x.Any()));
        analysisService.Received().PublishIssues(fileUri.LocalPath, analysisId, Arg.Is<IEnumerable<IAnalysisIssue>>(x => !x.Any()));
    }

    [TestMethod]
    public void RaiseFindings_NoSupportedLanguages_PublishesEmpty()
    {
        var analysisId = Guid.NewGuid();
        var fileUri = new FileUri("file://C:/somefile");
        var findingsByFileUri = new Dictionary<FileUri, List<TestFinding>>
            { { fileUri, [CreateTestFinding("csharpsquid:S100"), CreateTestFinding("csharpsquid:S101")] } };

        var raiseFindingParams = new RaiseFindingParams<TestFinding>("CONFIGURATION_ID", findingsByFileUri, false, analysisId);

        var analysisService = Substitute.For<IAnalysisService>();
        IRaiseFindingToAnalysisIssueConverter raiseFindingToAnalysisIssueConverter = CreateConverter(findingsByFileUri.Single().Key, [], []);

        var analysisStatusNotifierFactory = CreateAnalysisStatusNotifierFactory(out var analysisStatusNotifier, fileUri.LocalPath, analysisId);

        var testSubject = CreateTestSubject(analysisService: analysisService,
            raiseFindingToAnalysisIssueConverter: raiseFindingToAnalysisIssueConverter,
            analysisStatusNotifierFactory: analysisStatusNotifierFactory,
            slCoreConstantsProvider: CreateConstantsProviderWithLanguages([]));

        testSubject.RaiseFinding(raiseFindingParams);

        raiseFindingToAnalysisIssueConverter.Received().GetAnalysisIssues(fileUri, Arg.Is<IEnumerable<TestFinding>>(x => !x.Any()));
        analysisService.Received().PublishIssues(fileUri.LocalPath, analysisId, Arg.Is<IEnumerable<IAnalysisIssue>>(x => !x.Any()));
        analysisStatusNotifier.AnalysisFinished(0, TimeSpan.Zero);
    }

    [TestMethod]
    public void RaiseFindings_NoKnownLanguages_PublishesEmpty()
    {
        var analysisId = Guid.NewGuid();
        var fileUri = new FileUri("file://C:/somefile");
        var findingsByFileUri = new Dictionary<FileUri, List<TestFinding>>
            { { fileUri, [CreateTestFinding("csharpsquid:S100"), CreateTestFinding("csharpsquid:S101")] } };

        var raiseFindingParams = new RaiseFindingParams<TestFinding>("CONFIGURATION_ID", findingsByFileUri, false, analysisId);

        var analysisService = Substitute.For<IAnalysisService>();
        IRaiseFindingToAnalysisIssueConverter raiseFindingToAnalysisIssueConverter = CreateConverter(findingsByFileUri.Single().Key, [], []);

        var analysisStatusNotifierFactory = CreateAnalysisStatusNotifierFactory(out var analysisStatusNotifier, fileUri.LocalPath, analysisId);

        var testSubject = CreateTestSubject(analysisService: analysisService,
            raiseFindingToAnalysisIssueConverter: raiseFindingToAnalysisIssueConverter,
            analysisStatusNotifierFactory: analysisStatusNotifierFactory,
            slCoreConstantsProvider: CreateConstantsProviderWithLanguages([SloopLanguage.JAVA]));

        testSubject.RaiseFinding(raiseFindingParams);

        raiseFindingToAnalysisIssueConverter.Received().GetAnalysisIssues(fileUri, Arg.Is<IEnumerable<TestFinding>>(x => !x.Any()));
        analysisService.Received().PublishIssues(fileUri.LocalPath, analysisId, Arg.Is<IEnumerable<IAnalysisIssue>>(x => !x.Any()));
        analysisStatusNotifier.AnalysisFinished(0, TimeSpan.Zero);
    }

    [TestMethod]
    public void RaiseFindings_HasNoFileUri_FinishesAnalysis()
    {
        var analysisId = Guid.NewGuid();
        var analysisStatusNotifierFactory = CreateAnalysisStatusNotifierFactory(out var analysisStatusNotifier, null, analysisId);
        var analysisService = Substitute.For<IAnalysisService>();
        var testSubject = CreateTestSubject(analysisService: analysisService, analysisStatusNotifierFactory: analysisStatusNotifierFactory);

        var act = () =>
            testSubject.RaiseFinding(new RaiseFindingParams<TestFinding>("CONFIGURATION_ID", new Dictionary<FileUri, List<TestFinding>>(), false,
                analysisId));

        act.Should().NotThrow();
        analysisService.ReceivedCalls().Should().BeEmpty();
        analysisStatusNotifier.ReceivedCalls().Should().BeEmpty();
    }

    [TestMethod]
    public void RaiseFindings_HasIssuesNotIntermediate_PublishFindings()
    {
        var analysisId = Guid.NewGuid();
        var fileUri = new FileUri("file://C:/somefile");
        var analysisIssue1 = CreateAnalysisIssue("csharpsquid:S100");
        var analysisIssue2 = CreateAnalysisIssue("secrets:S1012");
        var filteredIssues = new[] { analysisIssue1, analysisIssue2 };

        var raisedFinding1 = CreateTestFinding("csharpsquid:S100");
        var raisedFinding2 = CreateTestFinding("javascript:S101");
        var raisedFinding3 = CreateTestFinding("secrets:S1012");
        var filteredRaisedFindings = new[] { raisedFinding1, raisedFinding3 };
        var raisedFindings = new List<TestFinding> { raisedFinding1, raisedFinding2, raisedFinding3 };

        var findingsByFileUri = new Dictionary<FileUri, List<TestFinding>> { { fileUri, raisedFindings } };

        var raiseFindingParams = new RaiseFindingParams<TestFinding>("CONFIGURATION_ID", findingsByFileUri, false, analysisId);

        var analysisService = Substitute.For<IAnalysisService>();
        IRaiseFindingToAnalysisIssueConverter raiseFindingToAnalysisIssueConverter =
            CreateConverter(findingsByFileUri.Single().Key, filteredRaisedFindings, filteredIssues);

        var analysisStatusNotifierFactory = CreateAnalysisStatusNotifierFactory(out var analysisStatusNotifier, fileUri.LocalPath, analysisId);

        var testSubject = CreateTestSubject(analysisService: analysisService,
            raiseFindingToAnalysisIssueConverter: raiseFindingToAnalysisIssueConverter,
            analysisStatusNotifierFactory: analysisStatusNotifierFactory,
            slCoreConstantsProvider: CreateConstantsProviderWithLanguages(SloopLanguage.SECRETS, SloopLanguage.CS));

        testSubject.RaiseFinding(raiseFindingParams);

        analysisService.Received(1).PublishIssues(fileUri.LocalPath, analysisId, filteredIssues);
        raiseFindingToAnalysisIssueConverter.Received(1).GetAnalysisIssues(findingsByFileUri.Single().Key, Arg.Is<IEnumerable<TestFinding>>(
            x => x.SequenceEqual(filteredRaisedFindings)));

        analysisStatusNotifierFactory.Received(1).Create("SLCoreAnalyzer", fileUri.LocalPath, analysisId);
        analysisStatusNotifier.Received(1).AnalysisFinished(2, TimeSpan.Zero);
    }

    [TestMethod]
    public void RaiseFindings_MultipleFiles_PublishFindingsForEachFile()
    {
        var analysisId = Guid.NewGuid();
        var fileUri1 = new FileUri("file://C:/somefile");
        var fileUri2 = new FileUri("file://C:/someOtherfile");
        var analysisIssue1 = CreateAnalysisIssue("secrets:S100");
        var analysisIssue2 = CreateAnalysisIssue("secrets:S101");

        var raisedFinding1 = CreateTestFinding("secrets:S100");
        var raisedFinding2 = CreateTestFinding("secrets:S101");

        var findingsByFileUri = new Dictionary<FileUri, List<TestFinding>> { { fileUri1, [raisedFinding1] }, { fileUri2, [raisedFinding2] } };

        var raiseFindingParams = new RaiseFindingParams<TestFinding>("CONFIGURATION_ID", findingsByFileUri, false, analysisId);

        var analysisService = Substitute.For<IAnalysisService>();
        var raiseFindingParamsToAnalysisIssueConverter = Substitute.For<IRaiseFindingToAnalysisIssueConverter>();
        raiseFindingParamsToAnalysisIssueConverter.GetAnalysisIssues(fileUri1, Arg.Any<IEnumerable<TestFinding>>()).Returns([analysisIssue1]);
        raiseFindingParamsToAnalysisIssueConverter.GetAnalysisIssues(fileUri2, Arg.Any<IEnumerable<TestFinding>>()).Returns([analysisIssue2]);

        var analysisStatusNotifierFactory = Substitute.For<IAnalysisStatusNotifierFactory>();

        var testSubject = CreateTestSubject(analysisService: analysisService,
            raiseFindingToAnalysisIssueConverter: raiseFindingParamsToAnalysisIssueConverter,
            analysisStatusNotifierFactory: analysisStatusNotifierFactory);

        testSubject.RaiseFinding(raiseFindingParams);

        analysisService.Received(1).PublishIssues(fileUri1.LocalPath, analysisId,
            Arg.Is<IEnumerable<IAnalysisIssue>>(x => x.SequenceEqual(new List<IAnalysisIssue> { analysisIssue1 })));
        analysisService.Received(1).PublishIssues(fileUri2.LocalPath, analysisId,
            Arg.Is<IEnumerable<IAnalysisIssue>>(x => x.SequenceEqual(new List<IAnalysisIssue> { analysisIssue2 })));

        analysisStatusNotifierFactory.Received(1).Create("SLCoreAnalyzer", fileUri1.LocalPath, analysisId);
        analysisStatusNotifierFactory.Received(1).Create("SLCoreAnalyzer", fileUri2.LocalPath, analysisId);
    }

    [TestMethod]
    public void RaiseFindings_HasIssuesIntermediate_DoNotPublishFindings()
    {
        var raisedFinding1 = CreateTestFinding("csharpsquid:S100");
        var raisedFinding2 = CreateTestFinding("javascript:S101");
        var raisedFinding3 = CreateTestFinding("secrets:S1012");
        var raisedFindings = new List<TestFinding> { raisedFinding1, raisedFinding2, raisedFinding3 };

        var findingsByFileUri = new Dictionary<FileUri, List<TestFinding>> { { new FileUri("file://C:/somefile"), raisedFindings } };
        var raiseFindingParams = new RaiseFindingParams<TestFinding>("CONFIGURATION_ID", findingsByFileUri, true, Guid.NewGuid());

        var analysisService = Substitute.For<IAnalysisService>();

        var testSubject = CreateTestSubject(analysisService: analysisService);

        testSubject.RaiseFinding(raiseFindingParams);

        analysisService.DidNotReceiveWithAnyArgs().PublishIssues(default, default, default);
    }

    private RaisedFindingProcessor CreateTestSubject(
        IAnalysisService analysisService = null,
        IRaiseFindingToAnalysisIssueConverter raiseFindingToAnalysisIssueConverter = null,
        IAnalysisStatusNotifierFactory analysisStatusNotifierFactory = null,
        ILogger logger = null,
        ISLCoreConstantsProvider slCoreConstantsProvider = null)
        => new(
            slCoreConstantsProvider ??
            CreateConstantsProviderWithLanguages(SloopLanguage.SECRETS, SloopLanguage.JS, SloopLanguage.TS, SloopLanguage.CSS),
            analysisService ?? Substitute.For<IAnalysisService>(),
            raiseFindingToAnalysisIssueConverter ?? Substitute.For<IRaiseFindingToAnalysisIssueConverter>(),
            analysisStatusNotifierFactory ?? Substitute.For<IAnalysisStatusNotifierFactory>(), logger ?? new TestLogger());
    
    private static IRaiseFindingToAnalysisIssueConverter CreateConverter(FileUri fileUri, IReadOnlyCollection<TestFinding> raisedFindingDtos,
        IAnalysisIssue[] findings)
    {
        var raiseFindingParamsToAnalysisIssueConverter = Substitute.For<IRaiseFindingToAnalysisIssueConverter>();
        raiseFindingParamsToAnalysisIssueConverter
            .GetAnalysisIssues(fileUri, Arg.Is<IEnumerable<TestFinding>>(x => x.SequenceEqual(raisedFindingDtos))).Returns(findings);
        return raiseFindingParamsToAnalysisIssueConverter;
    }
    
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

    private TestFinding CreateTestFinding(string ruleKey)
    {
        return new TestFinding(default, default, ruleKey, default, default, default, default, default, default, default, default, default, default, default, default);
    }

    private static IAnalysisIssue CreateAnalysisIssue(string ruleKey)
    {
        var analysisIssue1 = Substitute.For<IAnalysisIssue>();
        analysisIssue1.RuleKey.Returns(ruleKey);
        return analysisIssue1;
    }

    private record TestFinding(
        Guid id,
        string serverKey,
        string ruleKey,
        string primaryMessage,
        IssueSeverity severity,
        RuleType type,
        CleanCodeAttribute cleanCodeAttribute,
        List<ImpactDto> impacts,
        DateTimeOffset introductionDate,
        bool isOnNewCode,
        bool resolved,
        TextRangeDto textRange,
        List<IssueFlowDto> flows,
        List<QuickFixDto> quickFixes,
        string ruleDescriptionContextKey) :
        RaisedFindingDto(id,
            serverKey,
            ruleKey,
            primaryMessage,
            severity,
            type,
            cleanCodeAttribute,
            impacts,
            introductionDate,
            isOnNewCode,
            resolved,
            textRange,
            flows,
            quickFixes,
            ruleDescriptionContextKey);
}
