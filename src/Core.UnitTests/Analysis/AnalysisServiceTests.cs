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
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Core.UnitTests.Analysis;

[TestClass]
public class AnalysisServiceTests
{
    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<AnalysisService, IAnalysisService>(
            MefTestHelpers.CreateExport<IAnalyzerController>(),
            MefTestHelpers.CreateExport<IIssueConsumerStorage>(),
            MefTestHelpers.CreateExport<IScheduler>());
    }

    [TestMethod]
    public void MefCtor_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<AnalysisService>();
    }
    
    [TestMethod]
    public void ScheduleAnalysis_AnalysisScheduler_CachesIssueConsumer_And_RunsAnalyzerController()
    {
        var analysisId = Guid.NewGuid();
        var detectedLanguages = Substitute.For<IEnumerable<AnalysisLanguage>>();
        var issueConsumer = Substitute.For<IIssueConsumer>();
        var analyzerOptions = Substitute.For<IAnalyzerOptions>();
        var scheduler = CreateDefaultScheduler();
        var analyzerController = Substitute.For<IAnalyzerController>();
        var issueConsumerStorage = Substitute.For<IIssueConsumerStorage>();
        var testSubject = CreateTestSubject(analyzerController, issueConsumerStorage, scheduler);

        testSubject.ScheduleAnalysis("file/path", analysisId, detectedLanguages, issueConsumer, analyzerOptions);
    
        Received.InOrder(() =>
        {
           scheduler.Schedule("file/path", Arg.Any<Action<CancellationToken>>(), Arg.Any<int>());
           issueConsumerStorage.Set("file/path", analysisId, issueConsumer);
           analyzerController.ExecuteAnalysis("file/path", analysisId, detectedLanguages, issueConsumer, analyzerOptions, Arg.Any<CancellationToken>());
        });
    }
    
    [TestMethod]
    public void ScheduleAnalysis_JobCancelledBeforeStarting_DoesNotExecute()
    {
        var scheduler = CreateDefaultScheduler(true);
        var analyzerController = Substitute.For<IAnalyzerController>();
        var issueConsumerStorage = Substitute.For<IIssueConsumerStorage>();
        var testSubject = CreateTestSubject(analyzerController, issueConsumerStorage, scheduler);

        testSubject.ScheduleAnalysis("file/path", default, default, default, default);
    
        scheduler.Received().Schedule("file/path", Arg.Any<Action<CancellationToken>>(), Arg.Any<int>());
        issueConsumerStorage.DidNotReceiveWithAnyArgs().Set(default, default, default);
        analyzerController.DidNotReceiveWithAnyArgs().ExecuteAnalysis(default, default, default, default, default, default);
    }

    [TestMethod]
    [DataRow(-1, AnalysisService.DefaultAnalysisTimeoutMs)]
    [DataRow(0, AnalysisService.DefaultAnalysisTimeoutMs)]
    [DataRow(1, 1)]
    [DataRow(999, 999)]
    public void ScheduleAnalysis_ProvidesCorrectTimeout(int envSettingsResponse, int expectedTimeout)
    {
        try
        {
            Environment.SetEnvironmentVariable(EnvironmentSettings.AnalysisTimeoutEnvVar, envSettingsResponse.ToString());
            var scheduler = Substitute.For<IScheduler>();
            var testSubject = CreateTestSubject(scheduler: scheduler);
    
            testSubject.ScheduleAnalysis("file/path", default, default, default, default);
            
            scheduler.Received().Schedule("file/path", Arg.Any<Action<CancellationToken>>(), expectedTimeout);
        }
        finally
        {
            Environment.SetEnvironmentVariable(EnvironmentSettings.AnalysisTimeoutEnvVar, null);
        }
    
    }
    
    [TestMethod]
    public void ScheduleAnalysis_NoEnvironmentSettings_DefaultTimeout()
    {
        var scheduler = Substitute.For<IScheduler>();
        var testSubject = CreateTestSubject(scheduler: scheduler);
    
        testSubject.ScheduleAnalysis("file/path", default, default, default, default);
    
        scheduler.Received().Schedule("file/path", Arg.Any<Action<CancellationToken>>(), AnalysisService.DefaultAnalysisTimeoutMs);
    }

    [TestMethod]
    public void PublishIssues_NoConsumerInStorage_DoesNothing()
    {
        var issueConsumerStorage = CreateIssueConsumerStorageWithStoredItem(Guid.NewGuid(), null, false);
        var testSubject = CreateTestSubject(issueConsumerStorage:issueConsumerStorage);
        
        var act = () => testSubject.PublishIssues("file/path", Guid.NewGuid(), Substitute.For<IEnumerable<IAnalysisIssue>>());
        
        act.Should().NotThrow();
    }
    
    [TestMethod]
    public void PublishIssues_DifferentAnalysisId_DoesNothing()
    {
        var analysisId = Guid.NewGuid();
        var issueConsumer = Substitute.For<IIssueConsumer>();
        var issueConsumerStorage = CreateIssueConsumerStorageWithStoredItem(Guid.NewGuid(), issueConsumer, true);
        var testSubject = CreateTestSubject(issueConsumerStorage:issueConsumerStorage);
        
        testSubject.PublishIssues("file/path", analysisId, Substitute.For<IEnumerable<IAnalysisIssue>>());
        
        issueConsumer.DidNotReceiveWithAnyArgs().Accept(default, default);
    }
    
    [TestMethod]
    public void PublishIssues_MatchingConsumer_PublishesIssues()
    {
        var analysisId = Guid.NewGuid();
        var issueConsumer = Substitute.For<IIssueConsumer>();
        var issueConsumerStorage = CreateIssueConsumerStorageWithStoredItem(analysisId, issueConsumer, true);
        var testSubject = CreateTestSubject(issueConsumerStorage:issueConsumerStorage);
        var analysisIssues = Substitute.For<IEnumerable<IAnalysisIssue>>();

        testSubject.PublishIssues("file/path", analysisId, analysisIssues);
        
        issueConsumer.Received().Accept("file/path", analysisIssues);
    }
    
    [DataRow(true)]
    [DataRow(false)]
    [DataTestMethod]
    public void IsAnalysisSupported_CallsAnalyzerController(bool expected)
    {
        var analyzerController = Substitute.For<IAnalyzerController>();
        var detectedLanguages = Substitute.For<IEnumerable<AnalysisLanguage>>();
        analyzerController.IsAnalysisSupported(detectedLanguages).Returns(expected);
        var testSubject = CreateTestSubject(analyzerController:analyzerController);

        testSubject.IsAnalysisSupported(detectedLanguages).Should().Be(expected);
    }

    [TestMethod]
    public void CancelForFile_JobCancelledBeforeStarting_DoesNotExecute()
    {
        var issueConsumerStorage = Substitute.For<IIssueConsumerStorage>();
        var scheduler = CreateDefaultScheduler(true);
        var testSubject = CreateTestSubject(issueConsumerStorage: issueConsumerStorage, scheduler: scheduler);
        
        testSubject.CancelForFile("file/path");
        
        scheduler.Received().Schedule("file/path", Arg.Any<Action<CancellationToken>>(), -1);
        issueConsumerStorage.DidNotReceiveWithAnyArgs().Remove(default);
    }
    
    [TestMethod]
    public void CancelForFile_RunsConsumerStorageClearAsScheduledJob()
    {
        var issueConsumerStorage = Substitute.For<IIssueConsumerStorage>();
        var scheduler = CreateDefaultScheduler();
        var testSubject = CreateTestSubject(issueConsumerStorage: issueConsumerStorage, scheduler: scheduler);
        
        testSubject.CancelForFile("file/path");
        
        Received.InOrder(() =>
        {
            scheduler.Schedule("file/path", Arg.Any<Action<CancellationToken>>(), -1);
            issueConsumerStorage.Remove("file/path");
        });
    }

    private static IAnalysisService CreateTestSubject(IAnalyzerController analyzerController = null,
        IIssueConsumerStorage issueConsumerStorage = null,
        IScheduler scheduler = null)
    {
        analyzerController ??= Substitute.For<IAnalyzerController>();
        issueConsumerStorage ??= Substitute.For<IIssueConsumerStorage>();
        scheduler ??= Substitute.For<IScheduler>();
        return new AnalysisService(analyzerController, issueConsumerStorage, scheduler);
    }
    
    private static IIssueConsumerStorage CreateIssueConsumerStorageWithStoredItem(Guid analysisId, IIssueConsumer issueConsumer, bool result)
    {
        var issueConsumerStorage = Substitute.For<IIssueConsumerStorage>();
        issueConsumerStorage.TryGet("file/path", out Arg.Any<Guid>(), out Arg.Any<IIssueConsumer>()).Returns(info =>
        {
            info[1] = analysisId;
            info[2] = issueConsumer;
            return result;
        });
        return issueConsumerStorage;
    }
    
    private static IScheduler CreateDefaultScheduler(bool createCancelled = false)
    {
        var cancellationTokenSource = new CancellationTokenSource();
        if (createCancelled)
        {
            cancellationTokenSource.Cancel();
        }
        var scheduler = Substitute.For<IScheduler>();
        scheduler.When(x => x.Schedule(Arg.Any<string>(), Arg.Any<Action<CancellationToken>>(), Arg.Any<int>()))
            .Do(info => { info.Arg<Action<CancellationToken>>()(cancellationTokenSource.Token); });
        return scheduler;
    }
}
