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

using System;
using System.ComponentModel.Composition;
using System.Linq;
using StreamJsonRpc;

namespace SonarLint.VisualStudio.SLCore.Protocol
{
    /// <summary>
    /// Method name transformer for JsonRPC types.
    /// </summary>
    public interface IRpcMethodNameTransformer
    {
        /// <summary>
        /// Creates method name transformer (<see cref="JsonRpcProxyOptions.MethodNameTransform"/>) for the type.
        /// </summary>
        /// <returns>
        /// <typeparam name="TRpcType">Type of the RPC entity</typeparam>
        /// If <typeparamref name="TRpcType"/> is marked with <see cref="JsonRpcClassAttribute"/>, returns a composition of
        /// <see cref="CommonMethodNameTransforms.Prepend"/> with parameter <see cref="JsonRpcClassAttribute.Prefix"/> and <see cref="CommonMethodNameTransforms.CamelCase"/>.
        /// If not marked, returns <see cref="CommonMethodNameTransforms.CamelCase"/>
        /// </returns>
        Func<string, string> Create<TRpcType>();
    }

    [Export(typeof(IRpcMethodNameTransformer))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class RpcMethodNameTransformer : IRpcMethodNameTransformer
    {
        const string suffix = "Async";
        
        public Func<string, string> Create<T>()
        {
            if (!(typeof(T).GetCustomAttributes(typeof(JsonRpcClassAttribute), false).FirstOrDefault() is JsonRpcClassAttribute attribute))
            {
                return FormatName;
            }
            
            var prependTransform = CommonMethodNameTransforms.Prepend($"{attribute.Prefix}/");

            return name => prependTransform(FormatName(name));
        }

        private static string FormatName(string name) => CommonMethodNameTransforms.CamelCase(RemoveAsyncSuffix(name));

        private static string RemoveAsyncSuffix(string name) => 
            name.EndsWith(suffix) 
                ? name.Substring(0, name.Length - suffix.Length) 
                : name;
    }
}
