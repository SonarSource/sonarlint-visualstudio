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

using SonarLint.VisualStudio.ConnectedMode.QualityProfiles;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.ConnectedMode.Binding
{
    internal class BindingProcessImpl : IBindingProcess
    {
        private readonly BindCommandArgs bindingArgs;
        private readonly IQualityProfileDownloader qualityProfileDownloader;
        private readonly ILogger logger;

        public BindingProcessImpl(
            BindCommandArgs bindingArgs,
            IQualityProfileDownloader qualityProfileDownloader,
            ILogger logger)
        {
            this.bindingArgs = bindingArgs ?? throw new ArgumentNullException(nameof(bindingArgs));
            this.qualityProfileDownloader = qualityProfileDownloader ?? throw new ArgumentNullException(nameof(qualityProfileDownloader));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region IBindingTemplate methods

        public async Task<bool> DownloadQualityProfileAsync(IProgress<FixedStepsProgress> progress, CancellationToken cancellationToken)
        {
            try
            {
                await qualityProfileDownloader.UpdateAsync(bindingArgs.ProjectToBind, progress, cancellationToken);
                // ignore the UpdateAsync result, as the return value of false indicates error, rather than lack of changes
                return true;
            }
            catch (InvalidOperationException e)
            {
                logger.LogVerbose($"[{nameof(BindingProcessImpl)}] {e}");
            }

            return false;
        }

        #endregion
    }
}
