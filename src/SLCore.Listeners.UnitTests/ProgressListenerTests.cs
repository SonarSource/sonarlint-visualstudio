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

using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Listener.Progress;

namespace SonarLint.VisualStudio.SLCore.Listeners.UnitTests;

[TestClass]
public class ProgressListenerTests
{
    [TestMethod]
    public void MefCtor_CheckIsExported() => MefTestHelpers.CheckTypeCanBeImported<ProgressListener, ISLCoreListener>();

    [TestMethod]
    public void Mef_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<ProgressListener>();

    [TestMethod]
    [DataRow(null)]
    [DataRow("something")]
    public void StartProgress_ReturnsCompletedTaskAlways(string parameter)
    {
        var testSubject = new ProgressListener();

        var result = testSubject.StartProgressAsync(CreateStartProgressParam(parameter));

        result.Should().Be(Task.CompletedTask);
    }

    private static StartProgressParams CreateStartProgressParam(string title) => new(taskId: "id", title: title, message: null, configurationScopeId: null, indeterminate: true, cancellable: true);
}
