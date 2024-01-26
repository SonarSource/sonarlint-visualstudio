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

using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using StreamJsonRpc;

namespace SonarLint.VisualStudio.SLCore.Core
{
    /// <summary>
    /// A testable wrapper for JsonRpc. The implementation is expected to be thread-safe.
    /// </summary>
    internal interface IJsonRpc
    {
        T Attach<T>(JsonRpcProxyOptions options) where T : class;

        void AddLocalRpcTarget(object target, JsonRpcTargetOptions options);

        void StartListening();
        
        Task Completion { get; }
    }
    
    /// <summary>
    /// Wrapper for <see cref="JsonRpc"/> that implements <see cref="IJsonRpc"/>
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal class JsonRpcWrapper : JsonRpc, IJsonRpc
    {
        public JsonRpcWrapper(Stream sendingStream, Stream receivingStream) : base(sendingStream, receivingStream)
        {
        }
    }
}
