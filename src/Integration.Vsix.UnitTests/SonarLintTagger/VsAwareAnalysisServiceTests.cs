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

using Microsoft.VisualStudio.Text;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Integration.Vsix;
using SonarLint.VisualStudio.Integration.Vsix.Analysis;

namespace SonarLint.VisualStudio.Integration.UnitTests.SonarLintTagger;

[TestClass]
public class VsAwareAnalysisServiceTests
{
    private const string AnalysisFilePath = "analysis/file/path";
    private readonly ITextSnapshot analysisTextSnapshot = Substitute.For<ITextSnapshot>();
    private readonly SnapshotChangedHandler errorListHandler = Substitute.For<SnapshotChangedHandler>();
    private readonly (string projectName, Guid projectGuid) projectInfo = (projectName: "project123", projectGuid: Guid.NewGuid());
    private IAnalysisService analysisService;
    private IIssueConsumer issueConsumer;
    private IIssueConsumerFactory issueConsumerFactory;
    private IIssueConsumerStorage issueConsumerStorage;
    private VsAwareAnalysisService testSubject;
    private IThreadHandling threadHandling;
    private IVsProjectInfoProvider vsProjectInfoProvider;

    [TestInitialize]
    public void TestInitialize()
    {
        vsProjectInfoProvider = Substitute.For<IVsProjectInfoProvider>();
        issueConsumerFactory = Substitute.For<IIssueConsumerFactory>();
        issueConsumerStorage = Substitute.For<IIssueConsumerStorage>();
        issueConsumer = Substitute.For<IIssueConsumer>();
        analysisService = Substitute.For<IAnalysisService>();
        threadHandling = CreateDefaultThreadHandling();

        testSubject = new VsAwareAnalysisService(vsProjectInfoProvider, issueConsumerFactory, issueConsumerStorage, analysisService, threadHandling);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<VsAwareAnalysisService, IVsAwareAnalysisService>(
            MefTestHelpers.CreateExport<IVsProjectInfoProvider>(),
            MefTestHelpers.CreateExport<IIssueConsumerFactory>(),
            MefTestHelpers.CreateExport<IIssueConsumerStorage>(),
            MefTestHelpers.CreateExport<IAnalysisService>(),
            MefTestHelpers.CreateExport<IThreadHandling>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<VsAwareAnalysisService>();

    [TestMethod]
    public void CancelForFile_UsesAnalyzerService()
    {
        testSubject.CancelForFile("file/path");

        issueConsumerStorage.Received(1).Remove("file/path");
    }

    [TestMethod]
    public void RequestAnalysis_ProjectInformationReturned_CreatesIssueConsumerCorrectly()
    {
        var document = CreateDefaultDocument();
        MockDefaultIssueConsumerFactory(document);
        MockGetDocumentProjectInfo(projectInfo);

        testSubject.RequestAnalysis(document, new AnalysisSnapshot(AnalysisFilePath, analysisTextSnapshot), errorListHandler);

        issueConsumerFactory.Received(1).Create(document, AnalysisFilePath, analysisTextSnapshot, projectInfo.projectName, projectInfo.projectGuid,
            errorListHandler);
        issueConsumerStorage.Received(1).Set(AnalysisFilePath, issueConsumer);
    }

    [TestMethod]
    public void RequestAnalysis_NoProjectInformation_CreatesIssueConsumerCorrectly()
    {
        var document = CreateDefaultDocument();
        MockDefaultIssueConsumerFactory(document);
        MockGetDocumentProjectInfo(default);

        testSubject.RequestAnalysis(document, new AnalysisSnapshot(AnalysisFilePath, analysisTextSnapshot), errorListHandler);

        issueConsumerFactory.Received().Create(document, AnalysisFilePath, analysisTextSnapshot, default, Guid.Empty, errorListHandler);
        issueConsumerStorage.Received(1).Set(AnalysisFilePath, issueConsumer);
    }

    [TestMethod]
    public void RequestAnalysis_ClearsErrorListAndSchedulesAnalysisOnBackgroundThread()
    {
        var document = CreateDefaultDocument();
        MockDefaultIssueConsumerFactory(document);

        testSubject.RequestAnalysis(document, new AnalysisSnapshot(AnalysisFilePath, analysisTextSnapshot), default);

        Received.InOrder(() =>
        {
            threadHandling.RunOnBackgroundThread(Arg.Any<Func<Task<int>>>());
            issueConsumer.SetIssues(AnalysisFilePath, []);
            issueConsumer.SetHotspots(AnalysisFilePath, []);
            analysisService.ScheduleAnalysis(AnalysisFilePath);
        });
    }

    [TestMethod]
    public void RequestAnalysis_ProvidesAnalysisParametersCorrectly()
    {
        var document = CreateDefaultDocument();
        MockDefaultIssueConsumerFactory(document);

        testSubject.RequestAnalysis(document, new AnalysisSnapshot(AnalysisFilePath, analysisTextSnapshot), default);

        analysisService.Received(1).ScheduleAnalysis(AnalysisFilePath);
    }

    private static IThreadHandling CreateDefaultThreadHandling()
    {
        var mockThreadHandling = Substitute.For<IThreadHandling>();
        mockThreadHandling.RunOnBackgroundThread(Arg.Any<Func<Task<int>>>()).Returns(async info => await info.Arg<Func<Task<int>>>()());
        return mockThreadHandling;
    }

    private static ITextDocument CreateDefaultDocument()
    {
        var textDocument = Substitute.For<ITextDocument>();
        return textDocument;
    }

    private void MockGetDocumentProjectInfo((string projectName, Guid projectGuid) projectInfoToSet) => vsProjectInfoProvider.GetDocumentProjectInfoAsync(AnalysisFilePath).Returns(projectInfoToSet);

    private void MockDefaultIssueConsumerFactory(ITextDocument document) =>
        issueConsumerFactory
            .Create(document,
                Arg.Any<string>(),
                Arg.Any<ITextSnapshot>(),
                Arg.Any<string>(),
                Arg.Any<Guid>(),
                Arg.Any<SnapshotChangedHandler>())
            .Returns(issueConsumer);
}
