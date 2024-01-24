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

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.CFamily.CMake
{
    /// <summary>
    /// Returns the environment settings to be passed to the subprocess
    /// for an analysis request.
    /// </summary>
    internal interface IEnvironmentVarsProvider
    {
        Task<IReadOnlyDictionary<string, string>> GetAsync();
    }

    [Export(typeof(IEnvironmentVarsProvider))]
    internal class EnvironmentVarsProvider : IEnvironmentVarsProvider
    {
        private readonly IVsDevCmdEnvironmentProvider vsDevCmdProvider;
        private readonly ILogger logger;
        private readonly IDictionary<string, IReadOnlyDictionary<string, string>> cachedVsDevCmdSettings;

        private readonly SemaphoreSlim fetchLock = new SemaphoreSlim(1, 1);

        [ImportingConstructor]
        public EnvironmentVarsProvider(IVsDevCmdEnvironmentProvider vsDevCmdProvider, ILogger logger)
        {
            this.vsDevCmdProvider = vsDevCmdProvider;
            this.logger = logger;
            cachedVsDevCmdSettings = new Dictionary<string, IReadOnlyDictionary<string, string>>();
        }

        public async Task<IReadOnlyDictionary<string, string>> GetAsync()
        {
            // TODO - work out which script parameters to pass
            string vsDevCmdScriptParams = string.Empty;

            return await GetAsync(vsDevCmdScriptParams);
        }

        internal /* for testing */ async Task<IReadOnlyDictionary<string, string>> GetAsync(string vsDevCmdScriptParams)
        {
            // Try to get the cached settings
            if (cachedVsDevCmdSettings.TryGetValue(vsDevCmdScriptParams, out var cachedSettings))
            {
                LogDebug($"Cache hit. Script params: \"{vsDevCmdScriptParams}\"");
                return cachedSettings;
            }

            LogDebug($"Cache miss. Script params: \"{vsDevCmdScriptParams}\"");
            return await FetchAndCacheVsDevCmdSettingsAsync(vsDevCmdScriptParams);
        }

        private async Task<IReadOnlyDictionary<string, string>> FetchAndCacheVsDevCmdSettingsAsync(string vsDevCmdScriptParams)
        {
            LogDebug("\tWaiting to acquire lock...");

            await fetchLock.WaitAsync();

            try
            {
                LogDebug("\tAcquired lock.");
                // Re-check that the settings weren't fetch while we were waiting to obtain the lock
                if (cachedVsDevCmdSettings.TryGetValue(vsDevCmdScriptParams, out var cachedSettings))
                {
                    LogDebug($"Cache hit (inside lock). Script params: \"{vsDevCmdScriptParams}\"");
                    return cachedSettings;
                }

                var newSettings = await vsDevCmdProvider.GetAsync(vsDevCmdScriptParams);
                cachedVsDevCmdSettings[vsDevCmdScriptParams] = newSettings;

                return newSettings;
            }
            finally
            {
                fetchLock.Release();
            }
        }

        private void LogDebug(string message)
        {
            logger.LogVerbose($"[CMake:EnvVars] [Thread id: {Thread.CurrentThread.ManagedThreadId}] {message}");
        }
    }
}
