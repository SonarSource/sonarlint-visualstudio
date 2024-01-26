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

using SonarLint.VisualStudio.SLCore.Protocol;
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

        void StartListening();

        bool IsAlive { get; }
    }

    internal class SLCoreJsonRpc : ISLCoreJsonRpc
    {
        private readonly IJsonRpc rpc;
        private readonly IRpcMethodNameTransformer methodNameTransformer;

        public SLCoreJsonRpc(IJsonRpc jsonRpc, IRpcMethodNameTransformer methodNameTransformer)
        {
            rpc = jsonRpc;
            this.methodNameTransformer = methodNameTransformer;
        }

        public TService CreateService<TService>() where TService : class, ISLCoreService =>
            rpc.Attach<TService>(new JsonRpcProxyOptions
            {
                MethodNameTransform = methodNameTransformer.Create<TService>()
            });

        public void AttachListener(ISLCoreListener listener) =>
            rpc.AddLocalRpcTarget(listener,
                new JsonRpcTargetOptions
                {
                    MethodNameTransform = methodNameTransformer.Create<ISLCoreListener>(),
                    UseSingleObjectParameterDeserialization = true
                });

        public void StartListening()
        {
            rpc.StartListening();
        }

        public bool IsAlive => !rpc.Completion.IsCompleted;
    }
}
