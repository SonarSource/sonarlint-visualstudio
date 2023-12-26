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
using System.ComponentModel;
using System.ComponentModel.Composition;

namespace SonarLint.VisualStudio.SLCore.Core
{
    public interface ISLCoreServiceProvider
    {
        /// <summary>
        /// Gets a transient object representing an SLCore service. The object should not be cached
        /// </summary>
        /// <typeparam name="TService">An interface inherited from <see cref="ISLCoreService"/></typeparam>
        /// <returns>True if the underlying connection is alive, False if the connection is unavailable at the moment</returns>
        bool TryGetTransientService<TService>(out TService service) where TService : ISLCoreService;
    }

    public interface ISLCoreServiceProviderWriter 
    {
        /// <summary>
        /// Resets the state with a new <see cref="ISLCoreJsonRpc"/> instance and clears the cache
        /// </summary>
        void SetCurrentConnection(ISLCoreJsonRpc newRpcInstance);
    }

    [Export(typeof(ISLCoreServiceProvider))]
    [Export(typeof(ISLCoreServiceProviderWriter))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class SLCoreServiceProvider : ISLCoreServiceProvider, ISLCoreServiceProviderWriter
    {
        private readonly Dictionary<Type, object> cache = new Dictionary<Type, object>();
        private readonly object cacheLock = new object();
        private ISLCoreJsonRpc jsonRpc;
        
        public bool TryGetTransientService<TService>(out TService service) where TService : ISLCoreService
        {
            service = default;
            
            var serviceType = typeof(TService);
            if (!serviceType.IsInterface)
            {
                throw new ArgumentException($"The type argument {serviceType.FullName} is not an interface");
            }
            
            lock (cacheLock)
            {
                if (jsonRpc == null || !jsonRpc.IsAlive)
                {
                    return false;
                }

                if (!cache.TryGetValue(serviceType, out var cachedService))
                {
                    cachedService = jsonRpc.CreateService<TService>();
                    cache.Add(serviceType, cachedService);
                }

                service = (TService)cachedService;
                return true;
            }
        }

        public void SetCurrentConnection(ISLCoreJsonRpc newRpcInstance)
        {
            lock (cacheLock)
            {
                jsonRpc = newRpcInstance;
                cache.Clear();
            }
        }
    }
}
