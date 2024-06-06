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
            MefTestHelpers.CreateExport<IAnalyzerController>(),
            MefTestHelpers.CreateExport<IScheduler>(),
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
    public void IsAnalysisSupported_UsesAnalyzerController(bool isSupported)
    {
        var analysisLanguages = Substitute.For<IEnumerable<AnalysisLanguage>>();
        var analyzerController = Substitute.For<IAnalyzerController>();
        analyzerController.IsAnalysisSupported(analysisLanguages).Returns(isSupported);
        var testSubject = CreateTestSubject(analyzerController:analyzerController);

        testSubject.IsAnalysisSupported(analysisLanguages).Should().Be(isSupported);

        analyzerController.Received().IsAnalysisSupported(analysisLanguages);
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
        var testSubject = CreateTestSubject(issueConsumerFactory:issueConsumerFactory, projectInfoProvider:vsProjectInfoProvider);

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
        var testSubject = CreateTestSubject(issueConsumerFactory:issueConsumerFactory, projectInfoProvider:vsProjectInfoProvider);

        testSubject.RequestAnalysis("file/path", document, default, errorListHandler, default);

        issueConsumerFactory.Received().Create(document, default, default, errorListHandler);
    }
    
    [TestMethod]
    public void RequestAnalysis_ClearsErrorListAndSchedulesAnalysisOnBackgroundThread()
    {
        var document = CreateDefaultDocument();
        var scheduler = Substitute.For<IScheduler>();
        var threadHandling = CreateDefaultThreadHandling();
        var issueConsumerFactory = CreateDefaultIssueConsumerFactory(document, out var issueConsumer);
        var testSubject = CreateTestSubject(issueConsumerFactory: issueConsumerFactory, scheduler:scheduler, threadHandling:threadHandling);

        testSubject.RequestAnalysis("file/path", document, default, default, default);

        Received.InOrder(() =>
        {
            threadHandling.RunOnBackgroundThread(Arg.Any<Func<Task<int>>>());
            issueConsumer.Accept("file/path", Enumerable.Empty<IAnalysisIssue>());
            scheduler.Schedule("file/path", Arg.Any<Action<CancellationToken>>(), Arg.Any<int>());
        });
    }

    [TestMethod]
    public void RequestAnalysis_AnalysisSchedulerRunsAnalyzerController()
    {
        var documentEncoding = Encoding.BigEndianUnicode;
        var document = CreateDefaultDocument(documentEncoding);
        var detectedLanguages = Substitute.For<IEnumerable<AnalysisLanguage>>();
        var analyzerOptions = Substitute.For<IAnalyzerOptions>();
        var scheduler = CreateDefaultScheduler();
        var issueConsumerFactory = CreateDefaultIssueConsumerFactory(document, out var issueConsumer);
        var analyzerController = Substitute.For<IAnalyzerController>();
        var testSubject = CreateTestSubject(issueConsumerFactory: issueConsumerFactory, scheduler:scheduler, analyzerController:analyzerController);

        testSubject.RequestAnalysis("file/path", document, detectedLanguages, default, analyzerOptions);

        Received.InOrder(() =>
        {
           scheduler.Schedule("file/path", Arg.Any<Action<CancellationToken>>(), Arg.Any<int>());
           analyzerController.ExecuteAnalysis("file/path", documentEncoding.WebName, detectedLanguages, issueConsumer, analyzerOptions, Arg.Any<CancellationToken>());
        });
    }
    
    [TestMethod]
    [DataRow(-1, VsAwareAnalysisService.DefaultAnalysisTimeoutMs)]
    [DataRow(0, VsAwareAnalysisService.DefaultAnalysisTimeoutMs)]
    [DataRow(1, 1)]
    [DataRow(999, 999)]
    public void RequestAnalysis_ProvidesCorrectTimeout(int envSettingsResponse, int expectedTimeout)
    {
        try
        {
            Environment.SetEnvironmentVariable(EnvironmentSettings.AnalysisTimeoutEnvVar, envSettingsResponse.ToString());
            var scheduler = Substitute.For<IScheduler>();
            var testSubject = CreateTestSubject(scheduler:scheduler);
            
            testSubject.RequestAnalysis("file/path", CreateDefaultDocument(), default, default, default);
            
            scheduler.Received().Schedule("file/path", Arg.Any<Action<CancellationToken>>(), expectedTimeout);
        }
        finally
        {
            Environment.SetEnvironmentVariable(EnvironmentSettings.AnalysisTimeoutEnvVar, null);
        }
    
    }

    [TestMethod]
    public void RequestAnalysis_NoEnvironmentSettings_DefaultTimeout()
    {
        var scheduler = Substitute.For<IScheduler>();
        var testSubject = CreateTestSubject(scheduler: scheduler);

        testSubject.RequestAnalysis("file/path", CreateDefaultDocument(), default, default, default);

        scheduler.Received().Schedule("file/path", Arg.Any<Action<CancellationToken>>(), VsAwareAnalysisService.DefaultAnalysisTimeoutMs);
    }

    private static IVsAwareAnalysisService CreateTestSubject(IVsProjectInfoProvider projectInfoProvider = null,
        IIssueConsumerFactory issueConsumerFactory = null,
        IAnalyzerController analyzerController = null,
        IScheduler scheduler = null,
        IThreadHandling threadHandling = null)
    {
        projectInfoProvider ??= Substitute.For<IVsProjectInfoProvider>();
        issueConsumerFactory ??= Substitute.For<IIssueConsumerFactory>();
        analyzerController ??= Substitute.For<IAnalyzerController>();
        scheduler ??= Substitute.For<IScheduler>();
        threadHandling ??= new NoOpThreadHandler();
        return new VsAwareAnalysisService(projectInfoProvider, issueConsumerFactory, analyzerController, scheduler, threadHandling);
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

    private static IScheduler CreateDefaultScheduler()
    {
        var scheduler = Substitute.For<IScheduler>();
        scheduler.When(x => x.Schedule(Arg.Any<string>(), Arg.Any<Action<CancellationToken>>(), Arg.Any<int>()))
            .Do(info =>
            {
                info.Arg<Action<CancellationToken>>()(CancellationToken.None);
            });
        return scheduler;
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
