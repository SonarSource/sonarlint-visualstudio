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

using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.SLCore.Analysis;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Configuration;
using SonarLint.VisualStudio.SLCore.Listener.Analysis;
using SonarLint.VisualStudio.SLCore.Listener.Analysis.Models;
using SonarLint.VisualStudio.SLCore.Listeners.Implementation.Analysis;
using SonarLint.VisualStudio.SLCore.Protocol;
using SonarLint.VisualStudio.SLCore.Service.Rules.Models;
using SloopLanguage = SonarLint.VisualStudio.SLCore.Common.Models.Language;

namespace SonarLint.VisualStudio.SLCore.Listeners.UnitTests.Implementation.Analysis;

[TestClass]
public class RaisedFindingProcessorTests
{
    private const string FindingsType = "FINDING";

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<RaisedFindingProcessor, IRaisedFindingProcessor>(
            MefTestHelpers.CreateExport<ISLCoreConstantsProvider>(),
            MefTestHelpers.CreateExport<IRaiseFindingToAnalysisIssueConverter>(),
            MefTestHelpers.CreateExport<IAnalysisStatusNotifierFactory>(),
            MefTestHelpers.CreateExport<ILogger>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() =>
        MefTestHelpers.CheckIsSingletonMefComponent<RaisedFindingProcessor>();

    [TestMethod]
    public void RaiseFindings_AnalysisIdIsNull_Ignores()
    {
        var raiseFindingParams = new RaiseFindingParams<TestFinding>("CONFIGURATION_ID", new Dictionary<FileUri, List<TestFinding>>(), false, null);
        var publisher = Substitute.For<IFindingsPublisher>();
        var raiseFindingParamsToAnalysisIssueConverter = Substitute.For<IRaiseFindingToAnalysisIssueConverter>();

        var testSubject = CreateTestSubject(raiseFindingToAnalysisIssueConverter: raiseFindingParamsToAnalysisIssueConverter);

        testSubject.RaiseFinding(raiseFindingParams, publisher);

        publisher.DidNotReceiveWithAnyArgs().Publish(default, default, default);
        raiseFindingParamsToAnalysisIssueConverter.DidNotReceive().GetAnalysisIssues(Arg.Any<FileUri>(), Arg.Any<List<TestFinding>>());
    }

    [TestMethod]
    public void RaiseFindings_NoFindings_Ignores()
    {
        var fileUri = new FileUri("file://C:/somefile");
        var analysisId = Guid.NewGuid();
        var findingsByFileUri = new Dictionary<FileUri, List<TestFinding>> { { fileUri, [] } };

        var raiseFindingParams = new RaiseFindingParams<TestFinding>("CONFIGURATION_ID", findingsByFileUri, false, analysisId);
        var publisher = Substitute.For<IFindingsPublisher>();
        var raiseFindingParamsToAnalysisIssueConverter = CreateConverter(findingsByFileUri.Single().Key, [], []);

        var testSubject = CreateTestSubject(raiseFindingToAnalysisIssueConverter: raiseFindingParamsToAnalysisIssueConverter);

        testSubject.RaiseFinding(raiseFindingParams, publisher);

        raiseFindingParamsToAnalysisIssueConverter.Received().GetAnalysisIssues(fileUri, Arg.Is<IEnumerable<RaisedFindingDto>>(x => !x.Any()));
        publisher.Received().Publish(fileUri.LocalPath, analysisId, Arg.Is<IEnumerable<IAnalysisIssue>>(x => !x.Any()));
    }

    [TestMethod]
    public void RaiseFindings_NoKnownLanguages_PublishesEmpty()
    {
        var analysisId = Guid.NewGuid();
        var fileUri = new FileUri("file://C:/somefile");
        var findingsByFileUri = new Dictionary<FileUri, List<TestFinding>>
            { { fileUri, [CreateTestFinding("csharpsquid:S100"), CreateTestFinding("csharpsquid:S101")] } };

        var isIntermediatePublication = false;
        var raiseFindingParams = new RaiseFindingParams<TestFinding>("CONFIGURATION_ID", findingsByFileUri, isIntermediatePublication, analysisId);
        var publisher = CreatePublisher();
        IRaiseFindingToAnalysisIssueConverter raiseFindingToAnalysisIssueConverter = CreateConverter(findingsByFileUri.Single().Key, [], []);

        var analysisStatusNotifierFactory = CreateAnalysisStatusNotifierFactory(out var analysisStatusNotifier, fileUri.LocalPath, analysisId);
        var constantsProvider = CreateConstantsProviderWithLanguages([]);

        var testSubject = CreateTestSubject(raiseFindingToAnalysisIssueConverter: raiseFindingToAnalysisIssueConverter,
            analysisStatusNotifierFactory: analysisStatusNotifierFactory,
            slCoreConstantsProvider: constantsProvider);

        testSubject.RaiseFinding(raiseFindingParams, publisher);

        raiseFindingToAnalysisIssueConverter.Received().GetAnalysisIssues(fileUri, Arg.Is<IEnumerable<TestFinding>>(x => !x.Any()));
        publisher.Received().Publish(fileUri.LocalPath, analysisId, Arg.Is<IEnumerable<IAnalysisIssue>>(x => !x.Any()));
        analysisStatusNotifier.DidNotReceiveWithAnyArgs().AnalysisFinished(default);
        analysisStatusNotifier.Received().AnalysisProgressed(0, FindingsType, isIntermediatePublication);
        VerifyCorrectConstantsAreUsed(constantsProvider);
    }

    [TestMethod]
    public void RaiseFindings_NoSupportedLanguages_PublishesEmpty()
    {
        var analysisId = Guid.NewGuid();
        var fileUri = new FileUri("file://C:/somefile");
        var findingsByFileUri = new Dictionary<FileUri, List<TestFinding>>
            { { fileUri, [CreateTestFinding("csharpsquid:S100"), CreateTestFinding("csharpsquid:S101")] } };

        var isIntermediatePublication = false;
        var raiseFindingParams = new RaiseFindingParams<TestFinding>("CONFIGURATION_ID", findingsByFileUri, isIntermediatePublication, analysisId);
        var publisher = CreatePublisher();
        IRaiseFindingToAnalysisIssueConverter raiseFindingToAnalysisIssueConverter = CreateConverter(findingsByFileUri.Single().Key, [], []);

        var analysisStatusNotifierFactory = CreateAnalysisStatusNotifierFactory(out var analysisStatusNotifier, fileUri.LocalPath, analysisId);
        var constantsProvider = CreateConstantsProviderWithLanguages([SloopLanguage.JAVA]);

        var testSubject = CreateTestSubject(
            raiseFindingToAnalysisIssueConverter: raiseFindingToAnalysisIssueConverter,
            analysisStatusNotifierFactory: analysisStatusNotifierFactory,
            slCoreConstantsProvider: constantsProvider);

        testSubject.RaiseFinding(raiseFindingParams, publisher);

        raiseFindingToAnalysisIssueConverter.Received().GetAnalysisIssues(fileUri, Arg.Is<IEnumerable<TestFinding>>(x => !x.Any()));
        publisher.Received().Publish(fileUri.LocalPath, analysisId, Arg.Is<IEnumerable<IAnalysisIssue>>(x => !x.Any()));
        analysisStatusNotifier.DidNotReceiveWithAnyArgs().AnalysisFinished(default);
        analysisStatusNotifier.Received().AnalysisProgressed(0, FindingsType, isIntermediatePublication);
        VerifyCorrectConstantsAreUsed(constantsProvider);
    }

    [TestMethod]
    public void RaiseFindings_HasNoFileUri_FinishesAnalysis()
    {
        var analysisId = Guid.NewGuid();
        var analysisStatusNotifierFactory = CreateAnalysisStatusNotifierFactory(out var analysisStatusNotifier, null, analysisId);
        var publisher = CreatePublisher();
        var testSubject = CreateTestSubject(analysisStatusNotifierFactory: analysisStatusNotifierFactory);

        var act = () =>
            testSubject.RaiseFinding(new RaiseFindingParams<TestFinding>("CONFIGURATION_ID", new Dictionary<FileUri, List<TestFinding>>(), false,
                analysisId), publisher);

        act.Should().NotThrow();
        publisher.ReceivedCalls().Should().BeEmpty();
        analysisStatusNotifier.ReceivedCalls().Should().BeEmpty();
    }

    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void RaiseFindings_PublishFindings(bool isIntermediate)
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

        var raiseFindingParams = new RaiseFindingParams<TestFinding>("CONFIGURATION_ID", findingsByFileUri, isIntermediate, analysisId);
        var publisher = CreatePublisher();
        IRaiseFindingToAnalysisIssueConverter raiseFindingToAnalysisIssueConverter =
            CreateConverter(findingsByFileUri.Single().Key, filteredRaisedFindings, filteredIssues);

        var analysisStatusNotifierFactory = CreateAnalysisStatusNotifierFactory(out var analysisStatusNotifier, fileUri.LocalPath, analysisId);
        var constantsProvider = CreateConstantsProviderWithLanguages(SloopLanguage.SECRETS, SloopLanguage.CS);

        var testSubject = CreateTestSubject(
            raiseFindingToAnalysisIssueConverter: raiseFindingToAnalysisIssueConverter,
            analysisStatusNotifierFactory: analysisStatusNotifierFactory,
            slCoreConstantsProvider: constantsProvider);

        testSubject.RaiseFinding(raiseFindingParams, publisher);

        publisher.Received(1).Publish(fileUri.LocalPath, analysisId, filteredIssues);
        raiseFindingToAnalysisIssueConverter.Received(1).GetAnalysisIssues(findingsByFileUri.Single().Key, Arg.Is<IEnumerable<TestFinding>>(
            x => x.SequenceEqual(filteredRaisedFindings)));

        analysisStatusNotifierFactory.Received(1).Create("SLCoreAnalyzer", fileUri.LocalPath, analysisId);
        analysisStatusNotifier.DidNotReceiveWithAnyArgs().AnalysisFinished(default);
        analysisStatusNotifier.Received().AnalysisProgressed(2, FindingsType, isIntermediate);
        VerifyCorrectConstantsAreUsed(constantsProvider);
    }

    [DataRow(true)]
    [DataRow(false)]
    [DataTestMethod]
    public void RaiseFindings_MultipleFiles_PublishFindingsForEachFile(bool isIntermediate)
    {
        var analysisId = Guid.NewGuid();
        var fileUri1 = new FileUri("file://C:/somefile");
        var fileUri2 = new FileUri("file://C:/someOtherfile");
        var analysisIssue1 = CreateAnalysisIssue("secrets:S100");
        var analysisIssue2 = CreateAnalysisIssue("secrets:S101");

        var raisedFinding1 = CreateTestFinding("secrets:S100");
        var raisedFinding2 = CreateTestFinding("secrets:S101");

        var findingsByFileUri = new Dictionary<FileUri, List<TestFinding>> { { fileUri1, [raisedFinding1] }, { fileUri2, [raisedFinding2] } };

        var raiseFindingParams = new RaiseFindingParams<TestFinding>("CONFIGURATION_ID", findingsByFileUri, isIntermediate, analysisId);

        var publisher = CreatePublisher();
        var raiseFindingParamsToAnalysisIssueConverter = Substitute.For<IRaiseFindingToAnalysisIssueConverter>();
        raiseFindingParamsToAnalysisIssueConverter.GetAnalysisIssues(fileUri1, Arg.Any<IEnumerable<TestFinding>>()).Returns([analysisIssue1]);
        raiseFindingParamsToAnalysisIssueConverter.GetAnalysisIssues(fileUri2, Arg.Any<IEnumerable<TestFinding>>()).Returns([analysisIssue2]);

        var analysisStatusNotifierFactory = CreateAnalysisStatusNotifierFactory(out var notifier1, fileUri1.LocalPath, analysisId);
        SetUpNotifierForFile(out var notifier2, fileUri2.LocalPath, analysisId, analysisStatusNotifierFactory);

        var testSubject = CreateTestSubject(
            raiseFindingToAnalysisIssueConverter: raiseFindingParamsToAnalysisIssueConverter,
            analysisStatusNotifierFactory: analysisStatusNotifierFactory);

        testSubject.RaiseFinding(raiseFindingParams, publisher);

        publisher.Received(1).Publish(fileUri1.LocalPath, analysisId,
            Arg.Is<IEnumerable<IAnalysisIssue>>(x => x.SequenceEqual(new List<IAnalysisIssue> { analysisIssue1 })));
        publisher.Received(1).Publish(fileUri2.LocalPath, analysisId,
            Arg.Is<IEnumerable<IAnalysisIssue>>(x => x.SequenceEqual(new List<IAnalysisIssue> { analysisIssue2 })));

        analysisStatusNotifierFactory.Received(1).Create("SLCoreAnalyzer", fileUri1.LocalPath, analysisId);
        analysisStatusNotifierFactory.Received(1).Create("SLCoreAnalyzer", fileUri2.LocalPath, analysisId);
        notifier1.DidNotReceiveWithAnyArgs().AnalysisFinished(default);
        notifier1.Received().AnalysisProgressed(1, FindingsType, isIntermediate);
        notifier2.DidNotReceiveWithAnyArgs().AnalysisFinished(default);
        notifier2.Received().AnalysisProgressed(1, FindingsType, isIntermediate);
    }

    private RaisedFindingProcessor CreateTestSubject(
        IRaiseFindingToAnalysisIssueConverter raiseFindingToAnalysisIssueConverter = null,
        IAnalysisStatusNotifierFactory analysisStatusNotifierFactory = null,
        ILogger logger = null,
        ISLCoreConstantsProvider slCoreConstantsProvider = null)
        => new(
            slCoreConstantsProvider ??
            CreateConstantsProviderWithLanguages(SloopLanguage.SECRETS, SloopLanguage.JS, SloopLanguage.TS, SloopLanguage.CSS),
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
        slCoreConstantsProvider.AllAnalyzableLanguages.Returns(languages.ToList());
        return slCoreConstantsProvider;
    }

    private IAnalysisStatusNotifierFactory CreateAnalysisStatusNotifierFactory(out IAnalysisStatusNotifier analysisStatusNotifier, string filePath,
        Guid? analysisId)
    {
        var analysisStatusNotifierFactory = Substitute.For<IAnalysisStatusNotifierFactory>();
        SetUpNotifierForFile(out analysisStatusNotifier, filePath, analysisId, analysisStatusNotifierFactory);
        return analysisStatusNotifierFactory;
    }

    private static void SetUpNotifierForFile(
        out IAnalysisStatusNotifier analysisStatusNotifier,
        string filePath,
        Guid? analysisId,
        IAnalysisStatusNotifierFactory analysisStatusNotifierFactory)
    {
        analysisStatusNotifier = Substitute.For<IAnalysisStatusNotifier>();
        analysisStatusNotifierFactory.Create(nameof(SLCoreAnalyzer), filePath, analysisId).Returns(analysisStatusNotifier);
    }

    private TestFinding CreateTestFinding(string ruleKey)
    {
        return new TestFinding(default, default, ruleKey, default, default, default, default, default, default, default, default, default);
    }

    private static IFindingsPublisher CreatePublisher()
    {
        var publisher = Substitute.For<IFindingsPublisher>();
        publisher.FindingsType.Returns(FindingsType);
        return publisher;
    }

    private static IAnalysisIssue CreateAnalysisIssue(string ruleKey)
    {
        var analysisIssue1 = Substitute.For<IAnalysisIssue>();
        analysisIssue1.RuleKey.Returns(ruleKey);
        return analysisIssue1;
    }

    private static void VerifyCorrectConstantsAreUsed(ISLCoreConstantsProvider constantsProvider)
    {
        _ = constantsProvider.DidNotReceive().LanguagesInStandaloneMode;
        _ = constantsProvider.DidNotReceive().ExtraLanguagesInConnectedMode;
        _ = constantsProvider.DidNotReceive().LanguagesWithDisabledAnalysis;
        _ = constantsProvider.Received().AllAnalyzableLanguages;
    }

    private record TestFinding(
        Guid id,
        string serverKey,
        string ruleKey,
        string primaryMessage,
        DateTimeOffset introductionDate,
        bool isOnNewCode,
        bool resolved,
        TextRangeDto textRange,
        List<IssueFlowDto> flows,
        List<QuickFixDto> quickFixes,
        string ruleDescriptionContextKey,
        Either<StandardModeDetails, MQRModeDetails> severityMode) :
        RaisedFindingDto(id,
            serverKey,
            ruleKey,
            primaryMessage,
            introductionDate,
            isOnNewCode,
            resolved,
            textRange,
            flows,
            quickFixes,
            ruleDescriptionContextKey,
            severityMode);
}
