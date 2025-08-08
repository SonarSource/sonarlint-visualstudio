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

using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.SLCore.Service.Lifecycle;

namespace SonarLint.VisualStudio.SLCore.Core;

public interface ISLCoreServiceProvider
{
    /// <summary>
    /// Gets a transient object representing an SLCore service. The object should not be cached
    /// </summary>
    /// <typeparam name="TService">An interface inherited from <see cref="ISLCoreService"/></typeparam>
    /// <returns>True if the underlying connection is alive, False if the connection is unavailable at the moment or the backend hasn't been initialized</returns>
    bool TryGetTransientService<TService>([NotNullWhen(returnValue: true)] out TService? service) where TService : class, ISLCoreService;
}

public interface ISLCoreRpcManager
{
    void Initialize(InitializeParams parameters);

    bool IsInitialized { get; }

    void Shutdown();
}

internal interface ISLCoreServiceProviderWriter : ISLCoreServiceProvider
{
    /// <summary>
    /// Resets the state with a new <see cref="ISLCoreJsonRpc"/> instance and clears the cache
    /// </summary>
    void SetCurrentRpcInstance(ISLCoreJsonRpc newRpcInstance);
}

[Export(typeof(ISLCoreServiceProvider))]
[Export(typeof(ISLCoreRpcManager))]
[Export(typeof(ISLCoreServiceProviderWriter))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal class SLCoreServiceProvider(IThreadHandling threadHandling, ILogger logger) : ISLCoreServiceProviderWriter, ISLCoreRpcManager
{
    private readonly Dictionary<Type, object> cache = new();
    private readonly object locker = new();
    private bool backendInitialized;
    private ISLCoreJsonRpc? jsonRpc;

    public bool IsInitialized
    {
        get
        {
            lock (locker)
            {
                return backendInitialized;
            }
        }
    }

    public bool TryGetTransientService<TService>([NotNullWhen(returnValue: true)] out TService? service) where TService : class, ISLCoreService
    {
        threadHandling.ThrowIfOnUIThread();

        service = null;
        var serviceType = typeof(TService);
        if (!serviceType.IsInterface)
        {
            throw new ArgumentException($"The type argument {serviceType.FullName} is not an interface");
        }

        lock (locker)
        {
            if (!backendInitialized)
            {
                return false;
            }
            return TryGetTransientServiceInternal(out service);
        }
    }

    private bool TryGetTransientServiceInternal<TService>([NotNullWhen(returnValue: true)] out TService? service)
        where TService : class, ISLCoreService
    {
        service = null;
        var serviceType = typeof(TService);

        if (jsonRpc is not { IsAlive: true })
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

    public void SetCurrentRpcInstance(ISLCoreJsonRpc newRpcInstance)
    {
        threadHandling.ThrowIfOnUIThread();

        lock (locker)
        {
            jsonRpc = newRpcInstance;
            backendInitialized = false;
            cache.Clear();
        }
    }

    public void Initialize(InitializeParams parameters)
    {
        threadHandling.ThrowIfOnUIThread();

        lock (locker)
        {
            if (backendInitialized)
            {
                throw new InvalidOperationException(SLCoreStrings.BackendAlreadyInitialized);
            }

            if (!TryGetTransientServiceInternal(out ILifecycleManagementSLCoreService? lifecycleManagement))
            {
                throw new InvalidOperationException(SLCoreStrings.ServiceProviderNotInitialized);
            }

            lifecycleManagement.Initialize(parameters);
            backendInitialized = true;
        }
    }

    public void Shutdown()
    {
        threadHandling.ThrowIfOnUIThread();

        lock (locker)
        {
            if (TryGetTransientServiceInternal(out ILifecycleManagementSLCoreService? lifecycleManagement))
            {
                lifecycleManagement.Shutdown();
            }
            backendInitialized = false;
        }
    }
}
