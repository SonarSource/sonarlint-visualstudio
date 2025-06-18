﻿/*
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
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Listener.Analysis;
using SonarLint.VisualStudio.SLCore.Listener.Analysis.Models;
using SonarLint.VisualStudio.SLCore.Listeners.Implementation.Analysis;

namespace SonarLint.VisualStudio.SLCore.Listeners.UnitTests.Implementation.Analysis;

[TestClass]
public class AnalysisListenerTests
{
    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<AnalysisListener, ISLCoreListener>(
            MefTestHelpers.CreateExport<IActiveConfigScopeTracker>(),
            MefTestHelpers.CreateExport<IRaisedFindingProcessor>(),
            MefTestHelpers.CreateExport<IIssuePublisher>(),
            MefTestHelpers.CreateExport<IHotspotPublisher>(),
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
        var testLogger = new TestLogger();
        var testSubject = CreateTestSubject(activeConfigScopeTracker, logger: testLogger);

        testSubject.DidChangeAnalysisReadiness(new DidChangeAnalysisReadinessParams(new List<string> { "id" }, isReady));

        activeConfigScopeTracker.Received().TryUpdateAnalysisReadinessOnCurrentConfigScope("id", isReady);

        testLogger.AssertPartialOutputStringExists(string.Format(SLCoreStrings.AnalysisReadinessUpdate, isReady));
    }

    [TestMethod]
    public void RaiseIssues_RaisesFinding()
    {
        var raiseIssueParams = new RaiseFindingParams<RaisedIssueDto>(default, default, default, default);
        var raisedFindingProcessor = Substitute.For<IRaisedFindingProcessor>();
        var issuePublisher = Substitute.For<IIssuePublisher>();
        var testSubject = CreateTestSubject(raisedFindingProcessor: raisedFindingProcessor, issuePublisher: issuePublisher);

        testSubject.RaiseIssues(raiseIssueParams);

        raisedFindingProcessor.Received().RaiseFinding(raiseIssueParams, issuePublisher);
    }

    [TestMethod]
    public void RaiseHotspots_RaisesFinding()
    {
        var raiseIssueParams = new RaiseHotspotParams(default, default, default, default);
        var raisedFindingProcessor = Substitute.For<IRaisedFindingProcessor>();
        var hotspotPublisher = Substitute.For<IHotspotPublisher>();
        var testSubject = CreateTestSubject(raisedFindingProcessor: raisedFindingProcessor, hotspotPublisher: hotspotPublisher);

        testSubject.RaiseHotspots(raiseIssueParams);

        raisedFindingProcessor.Received().RaiseFinding(raiseIssueParams, hotspotPublisher);
    }

    private AnalysisListener CreateTestSubject(
        IActiveConfigScopeTracker activeConfigScopeTracker = null,
        IRaisedFindingProcessor raisedFindingProcessor = null,
        IIssuePublisher issuePublisher = null,
        IHotspotPublisher hotspotPublisher = null,
        ILogger logger = null) =>
        new(activeConfigScopeTracker ?? Substitute.For<IActiveConfigScopeTracker>(),
            raisedFindingProcessor ?? Substitute.For<IRaisedFindingProcessor>(),
            issuePublisher ?? Substitute.For<IIssuePublisher>(),
            hotspotPublisher ?? Substitute.For<IHotspotPublisher>(),
            logger ?? new TestLogger());
}
