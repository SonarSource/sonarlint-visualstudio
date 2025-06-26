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

using SonarLint.VisualStudio.Core.Notifications;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Listener.Progress;
using SonarLint.VisualStudio.SLCore.Protocol;

namespace SonarLint.VisualStudio.SLCore.Listeners.UnitTests;

[TestClass]
public class ProgressListenerTests
{
    private ProgressListener testSubject;
    private IStatusBarNotifier statusNotifier;

    [TestInitialize]
    public void TestInitialize()
    {
        statusNotifier = Substitute.For<IStatusBarNotifier>();
        testSubject = new ProgressListener(statusNotifier);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() => MefTestHelpers.CheckTypeCanBeImported<ProgressListener, ISLCoreListener>(MefTestHelpers.CreateExport<IStatusBarNotifier>());

    [TestMethod]
    public void Mef_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<ProgressListener>();

    [TestMethod]
    [DataRow(null)]
    [DataRow("something")]
    public void StartProgress_UpdatesStatus(string title)
    {
        var startProgressParams = CreateStartProgressParam(title);

        var result = testSubject.StartProgressAsync(startProgressParams);

        statusNotifier.Received(1).Notify(Arg.Is<string>(s => s == $"{SLCoreStrings.LongProductName}: {title}"), false);
        result.Should().Be(Task.CompletedTask);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("something")]
    public void ReportProgressAsync_ProgressUpdateNotification_UpdatesStatus(string title)
    {
        var reportProgressParam = CreateReportProgressParams(new ProgressUpdateNotification(title, 0));

        testSubject.ReportProgress(reportProgressParam);

        statusNotifier.Received(1).Notify(Arg.Is<string>(s => s == $"{SLCoreStrings.LongProductName}: {title}"), false);
    }

    [TestMethod]
    public void ReportProgressAsync_ProgressEndNotification_ClearsStatus()
    {
        var reportProgressParam = CreateReportProgressParams(new ProgressEndNotification());

        testSubject.ReportProgress(reportProgressParam);

        statusNotifier.Received(1).Notify(Arg.Is<string>(s => s == string.Empty), false);
    }

    private static StartProgressParams CreateStartProgressParam(string title) => new(taskId: "id", title: title, message: null, configurationScopeId: null, indeterminate: true, cancellable: true);

    private static ReportProgressParams CreateReportProgressParams(Either<ProgressUpdateNotification, ProgressEndNotification> notification) => new ReportProgressParams("id", notification);
}
