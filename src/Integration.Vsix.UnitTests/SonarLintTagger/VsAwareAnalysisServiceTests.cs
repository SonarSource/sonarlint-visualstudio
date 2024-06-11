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

using System.Text;
using Microsoft.VisualStudio.Text;
using NSubstitute.Extensions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Integration.Vsix;
using SonarLint.VisualStudio.Integration.Vsix.Analysis;

namespace SonarLint.VisualStudio.Integration.UnitTests.SonarLintTagger;

[TestClass]
public class VsAwareAnalysisServiceTests
{
    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<VsAwareAnalysisService, IVsAwareAnalysisService>(
            MefTestHelpers.CreateExport<IVsProjectInfoProvider>(),
            MefTestHelpers.CreateExport<IIssueConsumerFactory>(),
            MefTestHelpers.CreateExport<IAnalysisService>(),
            MefTestHelpers.CreateExport<IThreadHandling>());
    }

    [TestMethod]
    public void MefCtor_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<VsAwareAnalysisService>();
    }
    
    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void IsAnalysisSupported_UsesAnalyzerService(bool isSupported)
    {
        var detectedLanguages = Substitute.For<IEnumerable<AnalysisLanguage>>();
        var analysisService = Substitute.For<IAnalysisService>();
        analysisService.IsAnalysisSupported(detectedLanguages).Returns(isSupported);
        var testSubject = CreateTestSubject(analysisService:analysisService);

        testSubject.IsAnalysisSupported(detectedLanguages).Should().Be(isSupported);

        analysisService.Received().IsAnalysisSupported(detectedLanguages);
    }
    
    [TestMethod]
    public void CancelForFile_UsesAnalyzerService()
    {
        var analysisService = Substitute.For<IAnalysisService>();
        var testSubject = CreateTestSubject(analysisService:analysisService);

        testSubject.CancelForFile("file/path");

        analysisService.Received().CancelForFile("file/path");
    }

    [TestMethod]
    public void RequestAnalysis_ProjectInformationReturned_CreatesIssueConsumerCorrectly()
    {
        var projectInfo = (projectName: "project123", projectGuid: Guid.NewGuid());
        var document = CreateDefaultDocument();
        var errorListHandler = Substitute.For<SnapshotChangedHandler>();
        var vsProjectInfoProvider = Substitute.For<IVsProjectInfoProvider>();
        vsProjectInfoProvider.GetDocumentProjectInfoAsync(document).Returns(projectInfo);
        var issueConsumerFactory = Substitute.For<IIssueConsumerFactory>();
        var testSubject = CreateTestSubject(issueConsumerFactory: issueConsumerFactory, projectInfoProvider: vsProjectInfoProvider);

        testSubject.RequestAnalysis("file/path", document, default, errorListHandler, default);

        issueConsumerFactory.Received().Create(document, projectInfo.projectName, projectInfo.projectGuid, errorListHandler);
    }

    [TestMethod]
    public void RequestAnalysis_NoProjectInformation_CreatesIssueConsumerCorrectly()
    {
        var document = CreateDefaultDocument();
        var errorListHandler = Substitute.For<SnapshotChangedHandler>();
        var vsProjectInfoProvider = Substitute.For<IVsProjectInfoProvider>();
        vsProjectInfoProvider.GetDocumentProjectInfoAsync(document).Returns(default((string projectName, Guid projectGuid)));
        var issueConsumerFactory = Substitute.For<IIssueConsumerFactory>();
        var testSubject = CreateTestSubject(issueConsumerFactory: issueConsumerFactory, projectInfoProvider: vsProjectInfoProvider);

        testSubject.RequestAnalysis("file/path", document, default, errorListHandler, default);

        issueConsumerFactory.Received().Create(document, default, default, errorListHandler);
    }

    [TestMethod]
    public void RequestAnalysis_ClearsErrorListAndSchedulesAnalysisOnBackgroundThread()
    {
        var document = CreateDefaultDocument();
        var analysisService = Substitute.For<IAnalysisService>();
        var threadHandling = CreateDefaultThreadHandling();
        var issueConsumerFactory = CreateDefaultIssueConsumerFactory(document, out var issueConsumer);
        var testSubject = CreateTestSubject(issueConsumerFactory: issueConsumerFactory, analysisService: analysisService,
            threadHandling: threadHandling);

        testSubject.RequestAnalysis("file/path", document, default, default, default);

        Received.InOrder(() =>
        {
            threadHandling.RunOnBackgroundThread(Arg.Any<Func<Task<int>>>());
            issueConsumer.Accept("file/path", Enumerable.Empty<IAnalysisIssue>());
            analysisService.ScheduleAnalysis("file/path",
                Arg.Any<Guid>(),
                Arg.Any<string>(),
                Arg.Any<IEnumerable<AnalysisLanguage>>(),
                issueConsumer,
                Arg.Any<IAnalyzerOptions>());
        });
    }

    [TestMethod]
    public void RequestAnalysis_ProvidesAnalysisParametersCorrectly()
    {
        var encoding = Substitute.ForPartsOf<Encoding>();
        var encodingName = "encodingname";
        encoding.Configure().WebName.Returns(encodingName);
        var document = CreateDefaultDocument(encoding);
        var detectedLanguages = Substitute.For<IEnumerable<AnalysisLanguage>>();
        var analyzerOptions = Substitute.For<IAnalyzerOptions>();
        var analysisService = Substitute.For<IAnalysisService>();
        var issueConsumerFactory = CreateDefaultIssueConsumerFactory(document, out var issueConsumer);
        var testSubject = CreateTestSubject(issueConsumerFactory: issueConsumerFactory, analysisService: analysisService);

        testSubject.RequestAnalysis("file/path", document, detectedLanguages, default, analyzerOptions);

        analysisService
            .Received()
            .ScheduleAnalysis("file/path",
                Arg.Is<Guid>(x => x != Guid.Empty),
                encodingName,
                detectedLanguages,
                issueConsumer,
                analyzerOptions);
    }
    
    private static VsAwareAnalysisService CreateTestSubject(IVsProjectInfoProvider projectInfoProvider = null,
        IIssueConsumerFactory issueConsumerFactory = null,
        IAnalysisService analysisService = null,
        IThreadHandling threadHandling = null)
    {
        projectInfoProvider ??= Substitute.For<IVsProjectInfoProvider>();
        issueConsumerFactory ??= Substitute.For<IIssueConsumerFactory>();
        analysisService ??= Substitute.For<IAnalysisService>();
        threadHandling ??= new NoOpThreadHandler();
        return new VsAwareAnalysisService(projectInfoProvider, issueConsumerFactory, analysisService, threadHandling);
    }

    private static IThreadHandling CreateDefaultThreadHandling()
    {
        var threadHandling = Substitute.For<IThreadHandling>();
        threadHandling.RunOnBackgroundThread(Arg.Any<Func<Task<int>>>()).Returns(async info => await info.Arg<Func<Task<int>>>()());
        return threadHandling;
    }

    private static ITextDocument CreateDefaultDocument(Encoding charset = null)
    {
        var textDocument = Substitute.For<ITextDocument>();
        textDocument.Encoding.Returns(charset ?? Encoding.UTF8);
        return textDocument;
    }

    private static IIssueConsumerFactory CreateDefaultIssueConsumerFactory(ITextDocument document, out IIssueConsumer issueConsumer)
    {
        var issueConsumerFactory = Substitute.For<IIssueConsumerFactory>();
        issueConsumer = Substitute.For<IIssueConsumer>();
        issueConsumerFactory.Create(document, Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<SnapshotChangedHandler>())
            .Returns(issueConsumer);
        return issueConsumerFactory;
    }
}
