/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SonarLint.VisualStudio.ConnectedMode.Helpers;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Helpers;

[TestClass]
public class CancellableActionRunnerTests
{
    [TestMethod]
    public void MefCtor_CheckExports()
    {
        MefTestHelpers.CheckTypeCanBeImported<SynchronizedCancellableActionRunner, ICancellableActionRunner>(
            MefTestHelpers.CreateExport<ILogger>());
    }

    [TestMethod]
    public void CheckIsNonSharedMefComponent()
    {
        MefTestHelpers.CheckIsNonSharedMefComponent<SynchronizedCancellableActionRunner>();
    }

    [TestMethod]
    public async Task RunAsync_InvokesAction()
    {
        var ran = false;
        var testSubject = CreateTestSubject();
        
        await testSubject.RunAsync(_ => { ran = true; return Task.CompletedTask; });

        ran.Should().BeTrue();
    }
    
    [TestMethod]
    public async Task RunAsync_CancelsPreviousAction()
    {
        CancellationToken actionToken;
        var testSubject = CreateTestSubject();
        
        await testSubject.RunAsync(token => { actionToken = token; return Task.CompletedTask; });

        actionToken.IsCancellationRequested.Should().BeFalse();

        await testSubject.RunAsync(_ => Task.CompletedTask );

        actionToken.IsCancellationRequested.Should().BeTrue();
    }
    
        
    [TestMethod]
    public async Task RunAsync_Run100Actions_CancelsAllTokensButLatest()
    {
        var tokens = new List<CancellationToken>(100);
        var tasks = new List<Task>(100);
        var testSubject = CreateTestSubject();

        for (var i = 0; i < 100; i++)
        {
            tasks.Add(Task.Run(() => testSubject.RunAsync(ct => { tokens.Add(ct); return Task.CompletedTask; })));
        }

        await Task.WhenAll(tasks);
        
        var cacelled = tokens.Count(token => token.IsCancellationRequested);
        cacelled.Should().Be(99);
    }

    [TestMethod]
    public async Task Dispose_CancelsLastAction()
    {
        CancellationToken actionToken;
        var testSubject = CreateTestSubject();
        
        await testSubject.RunAsync(token => { actionToken = token; return Task.CompletedTask; });

        actionToken.IsCancellationRequested.Should().BeFalse();
        
        testSubject.Dispose();

        actionToken.IsCancellationRequested.Should().BeTrue();
    }
    
    [TestMethod]
    public void Dispose_PreventsNewActionLaunch()
    {
        var ran = false;
        var testSubject = CreateTestSubject();
        
        testSubject.Dispose();
        Func<Task> action =() => testSubject.RunAsync(_ => { ran = true; return Task.CompletedTask; });

        ran.Should().BeFalse();
        action.Should().ThrowAsync<ObjectDisposedException>();
    }

    private ICancellableActionRunner CreateTestSubject()
    {
        return new SynchronizedCancellableActionRunner(new TestLogger());
    }
}
