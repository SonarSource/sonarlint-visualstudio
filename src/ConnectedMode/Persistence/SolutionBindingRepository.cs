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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using SonarLint.VisualStudio.ConnectedMode.Binding;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;

namespace SonarLint.VisualStudio.ConnectedMode.Persistence
{
    [Export(typeof(ISolutionBindingRepository))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class SolutionBindingRepository : ISolutionBindingRepository
    {
        private readonly ISolutionBindingFileLoader solutionBindingFileLoader;
        private readonly ISolutionBindingCredentialsLoader credentialsLoader;
        private readonly IUnintrusiveBindingPathProvider unintrusiveBindingPathProvider;

        [ImportingConstructor]
        public SolutionBindingRepository(IUnintrusiveBindingPathProvider unintrusiveBindingPathProvider, ICredentialStoreService credentialStoreService, ILogger logger)
            : this(unintrusiveBindingPathProvider, new SolutionBindingFileLoader(logger), new SolutionBindingCredentialsLoader(credentialStoreService))
        {
        }

        internal /* for testing */ SolutionBindingRepository(IUnintrusiveBindingPathProvider unintrusiveBindingPathProvider, ISolutionBindingFileLoader solutionBindingFileLoader, ISolutionBindingCredentialsLoader credentialsLoader)
        {
            this.solutionBindingFileLoader = solutionBindingFileLoader ?? throw new ArgumentNullException(nameof(solutionBindingFileLoader));
            this.credentialsLoader = credentialsLoader ?? throw new ArgumentNullException(nameof(credentialsLoader));
            this.unintrusiveBindingPathProvider = unintrusiveBindingPathProvider;
        }

        public BoundSonarQubeProject Read(string configFilePath)
        {
            return Read(configFilePath, true);
        }

        public bool Write(string configFilePath, BoundSonarQubeProject binding)
        {
            _ = binding ?? throw new ArgumentNullException(nameof(binding));

            if (string.IsNullOrEmpty(configFilePath))
            {
                return false;
            }

            if (!solutionBindingFileLoader.Save(configFilePath, binding))
            {
                return false;
            }

            credentialsLoader.Save(binding.Credentials, binding.ServerUri);

            BindingUpdated?.Invoke(this, EventArgs.Empty);
            
            return true;
        }

        public event EventHandler BindingUpdated;

        public IEnumerable<BoundSonarQubeProject> List()
        {
            var bindingConfigPaths = unintrusiveBindingPathProvider.GetBindingPaths();

            foreach (var bindingConfigPath in bindingConfigPaths)
            {
                var boundSonarQubeProject = Read(bindingConfigPath, false);

                if (boundSonarQubeProject == null) { continue; }

                yield return boundSonarQubeProject;
            }
        }

        private BoundSonarQubeProject Read(string configFilePath, bool loadCredentials)
        {
            var bound = solutionBindingFileLoader.Load(configFilePath);

            if (bound is null)
            {
                return null;
            }

            if (loadCredentials)
            {
                bound.Credentials = credentialsLoader.Load(bound.ServerUri);
            }

            Debug.Assert(!bound.Profiles?.ContainsKey(Core.Language.Unknown) ?? true,
                "Not expecting the deserialized binding config to contain the profile for an unknown language");

            return bound;
        }
    }
}
