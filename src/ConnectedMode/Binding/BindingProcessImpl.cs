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
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using SonarLint.VisualStudio.ConnectedMode.Persistence;
using SonarLint.VisualStudio.ConnectedMode.QualityProfiles;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration;
using SonarQube.Client;
using SonarQube.Client.Models;
using Language = SonarLint.VisualStudio.Core.Language;

namespace SonarLint.VisualStudio.ConnectedMode.Binding
{
    internal class BindingProcessImpl : IBindingProcess
    {
        private readonly BindCommandArgs bindingArgs;
        private readonly IExclusionSettingsStorage exclusionSettingsStorage;
        private readonly ISonarQubeService sonarQubeService;
        private readonly IQualityProfileDownloader qualityProfileDownloader;
        private readonly ILogger logger;

        public BindingProcessImpl(
            BindCommandArgs bindingArgs,
            IExclusionSettingsStorage exclusionSettingsStorage,
            ISonarQubeService sonarQubeService,
            IQualityProfileDownloader qualityProfileDownloader,
            ILogger logger,
            bool isFirstBinding = false)
        {
            this.bindingArgs = bindingArgs ?? throw new ArgumentNullException(nameof(bindingArgs));
            this.exclusionSettingsStorage = exclusionSettingsStorage ?? throw new ArgumentNullException(nameof(exclusionSettingsStorage));
            this.sonarQubeService = sonarQubeService ?? throw new ArgumentNullException(nameof(sonarQubeService));
            this.qualityProfileDownloader = qualityProfileDownloader ?? throw new ArgumentNullException(nameof(qualityProfileDownloader));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            Debug.Assert(bindingArgs.ProjectKey != null);
            Debug.Assert(bindingArgs.ProjectName != null);
            Debug.Assert(bindingArgs.Connection != null);

            this.InternalState = new BindingProcessState(isFirstBinding);
        }

        // This property should not be used by product code outside this class
        internal /* for testing */ BindingProcessState InternalState { get;  }

        #region IBindingTemplate methods

        public async Task<bool> DownloadQualityProfileAsync(IProgress<FixedStepsProgress> progress, CancellationToken cancellationToken)
        {
            var boundProject = CreateNewBindingConfig();

            try
            {
                await qualityProfileDownloader.UpdateAsync(boundProject, progress, cancellationToken);
                // ignore the UpdateAsync result, as the return value of false indicates error, rather than lack of changes
                return true;
            }
            catch (InvalidOperationException e)
            {
                logger.LogVerbose($"[{nameof(BindingProcessImpl)}] {e}");
            }

            return false;
        }

        private BoundSonarQubeProject CreateNewBindingConfig()
        {
            BasicAuthCredentials credentials = bindingArgs.Connection.UserName == null ? null : new BasicAuthCredentials(bindingArgs.Connection.UserName, bindingArgs.Connection.Password);

            var boundProject = new BoundSonarQubeProject(bindingArgs.Connection.ServerUri,
                bindingArgs.ProjectKey,
                bindingArgs.ProjectName,
                credentials,
                bindingArgs.Connection.Organization);

            return boundProject;
        }

        public async Task<bool> SaveServerExclusionsAsync(CancellationToken cancellationToken)
        {
            try
            {
                var exclusions = await sonarQubeService.GetServerExclusions(bindingArgs.ProjectKey, cancellationToken);
                exclusionSettingsStorage.SaveSettings(exclusions);
            }
            catch(Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine(string.Format(BindingStrings.SaveExclusionsFailed, ex.Message));
                return false;
            }
            return true;
        }

        public bool BindOperationSucceeded => InternalState.BindingOperationSucceeded;

        #endregion

        #region Workflow state

        internal class BindingProcessState
        {
            public BindingProcessState(bool isFirstBinding)
            {
                this.IsFirstBinding = isFirstBinding;
            }

            public bool IsFirstBinding { get; }

            // TODO - change to simple list of configs
            public Dictionary<Language, IBindingConfig> BindingConfigs
            {
                get;
            } = new Dictionary<Language, IBindingConfig>();

            public Dictionary<Language, SonarQubeQualityProfile> QualityProfiles
            {
                get;
            } = new Dictionary<Language, SonarQubeQualityProfile>();

            public bool BindingOperationSucceeded
            {
                get;
                set;
            } = true;
        }

        #endregion
    }
}
