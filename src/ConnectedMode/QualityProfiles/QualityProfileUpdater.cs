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
using System.ComponentModel.Composition;
using System.Threading;
using SonarLint.VisualStudio.ConnectedMode.Helpers;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration;
using Task = System.Threading.Tasks.Task;

namespace SonarLint.VisualStudio.ConnectedMode.QualityProfiles
{
    internal interface IQualityProfileUpdater : INotifyQualityProfilesChanged
    {
        /// <summary>
        /// When in Connected Mode, ensures that all of the Quality Profiles are up to date
        /// </summary>
        Task UpdateAsync();
    }

    [Export(typeof(IQualityProfileUpdater))]
    [Export(typeof(INotifyQualityProfilesChanged))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class QualityProfileUpdater : IQualityProfileUpdater, IDisposable
    {
        private readonly IConfigurationProvider configProvider;
        private readonly IQualityProfileDownloader qualityProfileDownloader;
        private readonly ICancellableActionRunner runner;
        private readonly ILogger logger;

        public event EventHandler QualityProfilesChanged;

        [ImportingConstructor]
        public QualityProfileUpdater(IConfigurationProvider configProvider,
            IQualityProfileDownloader qualityProfileDownloader,
            ICancellableActionRunner runner,
            ILogger logger)
        {
            this.configProvider = configProvider;
            this.qualityProfileDownloader = qualityProfileDownloader;
            this.runner = runner;
            this.logger = logger;
        }

        public async Task UpdateAsync()
        {
            var config = configProvider.GetConfiguration();
            if (config.Mode != SonarLintMode.Connected)
            {
                logger.LogVerbose($"[QualityProfiles] Skipping Quality Profile update. Solution is not bound. Mode: {config.Mode}");
                return;
            }

            try
            {
                await runner.RunAsync(async token =>
                {
                    if (await qualityProfileDownloader.UpdateAsync(config.Project, null, token))
                    {
                        QualityProfilesChanged?.Invoke(this, EventArgs.Empty);
                    }
                });
            }
            catch (Exception e) when (e is OperationCanceledException || e is InvalidOperationException)
            {
                // no-op - job was cancelled
                logger.LogVerbose(e.ToString());
            }
        }

        public void Dispose() => runner.Dispose();
    }
}
