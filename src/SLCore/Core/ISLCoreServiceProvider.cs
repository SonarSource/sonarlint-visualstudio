﻿/*
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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.SLCore.Core
{
    public interface ISLCoreServiceProvider
    {
        /// <summary>
        /// Gets a transient object representing an SLCore service. The object should not be cached
        /// </summary>
        /// <typeparam name="TService">An interface inherited from <see cref="ISLCoreService"/></typeparam>
        /// <returns>True if the underlying connection is alive, False if the connection is unavailable at the moment</returns>
        bool TryGetTransientService<TService>(out TService service) where TService : class, ISLCoreService;
    }

    internal interface ISLCoreServiceProviderWriter : ISLCoreServiceProvider
    {
        /// <summary>
        /// Resets the state with a new <see cref="ISLCoreJsonRpc"/> instance and clears the cache
        /// </summary>
        void SetCurrentConnection(ISLCoreJsonRpc newRpcInstance);
    }

    [Export(typeof(ISLCoreServiceProvider))]
    [Export(typeof(ISLCoreServiceProviderWriter))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class SLCoreServiceProvider : ISLCoreServiceProviderWriter
    {

        private readonly Dictionary<Type, object> cache = new Dictionary<Type, object>();
        private readonly object cacheLock = new object();
        private ISLCoreJsonRpc jsonRpc;
        private readonly IThreadHandling threadHandling;
        private readonly ILogger logger;

        [ImportingConstructor]
        public SLCoreServiceProvider(IThreadHandling threadHandling, ILogger logger)
        {
            this.threadHandling = threadHandling;
            this.logger = logger;
        }

        public bool TryGetTransientService<TService>(out TService service) where TService : class, ISLCoreService
        {
            threadHandling.ThrowIfOnUIThread();

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
                    try
                    {
                        cachedService = jsonRpc.CreateService<TService>();
                    }
                    catch (Exception ex)
                    {
                        logger.WriteLine(SLCoreStrings.SLCoreServiceProvider_CreateServiceError, ex.Message);
                        return false;
                    }
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
