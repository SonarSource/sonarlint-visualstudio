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

using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using SonarLint.VisualStudio.Core.Synchronization;

namespace SonarLint.VisualStudio.Infrastructure.VS;


[Export(typeof(IAsyncLockFactory))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal class AsyncLockFactory : IAsyncLockFactory
{
    public IAsyncLock Create()
    {
        return new AsyncLock();
    }
}

[ExcludeFromCodeCoverage]
internal sealed class AsyncLock : IAsyncLock
{
    private readonly SemaphoreSlim semaphoreSlim = new (1, 1);

    public IReleaseAsyncLock Acquire()
    {
        semaphoreSlim.Wait();

        return new AsyncLockToken(this);
    }

    public async Task<IReleaseAsyncLock> AcquireAsync()
    {
        await semaphoreSlim.WaitAsync();

        return new AsyncLockToken(this);
    }

    private void Release() => semaphoreSlim.Release();
    
    public void Dispose() => semaphoreSlim.Dispose();
    
    private sealed class AsyncLockToken : IReleaseAsyncLock
    {
        private readonly AsyncLock asyncLock;

        public AsyncLockToken(AsyncLock asyncLock)
        {
            this.asyncLock = asyncLock;
        }
        
        public void Dispose()
        {
            asyncLock.Release();
        }
    }
}
