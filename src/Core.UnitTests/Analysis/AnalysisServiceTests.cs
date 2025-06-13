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

using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Core.UnitTests.Analysis;

[TestClass]
public class AnalysisServiceTests
{
    private IAnalyzer analyzer;
    private CancellationTokenSource cancellationTokenSource;
    private IScheduler scheduler;
    private IAnalysisService testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        analyzer = Substitute.For<IAnalyzer>();
        MockScheduler();

        testSubject = new AnalysisService(analyzer, scheduler);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<AnalysisService, IAnalysisService>(
            MefTestHelpers.CreateExport<IAnalyzer>(),
            MefTestHelpers.CreateExport<IScheduler>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<AnalysisService>();

    [TestMethod]
    public void ScheduleAnalysis_AnalysisScheduler_RunsAnalyzer()
    {
        testSubject.ScheduleAnalysis("file/path");

        Received.InOrder(() =>
        {
            scheduler.Schedule("file/path", Arg.Any<Action<CancellationToken>>(), Arg.Any<int>());
            analyzer.ExecuteAnalysis(Arg.Is<List<string>>(x => x.SequenceEqual(new List<string> { "file/path" })));
        });
    }

    [TestMethod]
    public void ScheduleAnalysis_JobCancelledBeforeStarting_DoesNotExecute()
    {
        cancellationTokenSource.Cancel();

        testSubject.ScheduleAnalysis("file/path");

        scheduler.Received().Schedule("file/path", Arg.Any<Action<CancellationToken>>(), Arg.Any<int>());
        analyzer.DidNotReceiveWithAnyArgs().ExecuteAnalysis(default);
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

            testSubject.ScheduleAnalysis("file/path");

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
        testSubject.ScheduleAnalysis("file/path");

        scheduler.Received().Schedule("file/path", Arg.Any<Action<CancellationToken>>(), AnalysisService.DefaultAnalysisTimeoutMs);
    }

    private void MockScheduler()
    {
        scheduler = Substitute.For<IScheduler>();
        cancellationTokenSource = new CancellationTokenSource();
        scheduler.When(x => x.Schedule(Arg.Any<string>(), Arg.Any<Action<CancellationToken>>(), Arg.Any<int>()))
            .Do(info => { info.Arg<Action<CancellationToken>>()(cancellationTokenSource.Token); });
    }
}
