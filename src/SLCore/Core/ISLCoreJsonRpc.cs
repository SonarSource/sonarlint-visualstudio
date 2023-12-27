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
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;
using StreamJsonRpc;

namespace SonarLint.VisualStudio.SLCore.Core
{
    /// <summary>
    /// A friendly wrapper for JsonRpc connection.
    /// </summary>
    public interface ISLCoreJsonRpc
    {
        TService CreateService<TService>() where TService : class, ISLCoreService;

        void AttachListener(ISLCoreListener listener);

        bool IsAlive { get; }
    }

    internal class SLCoreJsonRpc : ISLCoreJsonRpc
    {
        private readonly object lockObject = new object();
        private readonly IJsonRpc rpc;
        private bool isAlive = true;

        public SLCoreJsonRpc(IJsonRpc jsonRpc)
        {
            rpc = jsonRpc;
            AwaitRpcCompletionAsync().Forget();
        }

        public TService CreateService<TService>() where TService : class, ISLCoreService
        {
            lock (lockObject)
            {
                return rpc.Attach<TService>(new JsonRpcProxyOptions
                    { MethodNameTransform = CommonMethodNameTransforms.CamelCase }); // todo: https://github.com/SonarSource/sonarlint-visualstudio/issues/5140
            }
        }

        public void AttachListener(ISLCoreListener listener)
        {
            lock (lockObject)
            {
                rpc.AddLocalRpcTarget(listener,
                    new JsonRpcTargetOptions
                    {
                        MethodNameTransform = CommonMethodNameTransforms.CamelCase,
                        UseSingleObjectParameterDeserialization = true
                    });
            }
        }

        public bool IsAlive
        {
            get
            {
                lock (lockObject)
                {
                    return isAlive;
                }
            }
        }

        private async Task AwaitRpcCompletionAsync()
        {
            try
            {
                await rpc.Completion;
            }
            catch (Exception)
            {
                // we want to set isAlive to false on any exception here, including TaskCanceledException
            }
            
            lock (lockObject)
            {
                isAlive = false;
            }
        }
    }
}
