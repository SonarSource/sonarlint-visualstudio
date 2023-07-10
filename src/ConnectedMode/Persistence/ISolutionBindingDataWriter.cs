﻿/*
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
using SonarLint.VisualStudio.ConnectedMode.Binding;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration;

namespace SonarLint.VisualStudio.ConnectedMode.Persistence
{
    internal interface ISolutionBindingDataWriter
    {
        /// <summary>
        /// Writes the binding information
        /// </summary>
        /// <returns>Has file been saved</returns>
        bool Write(string configFilePath, BoundSonarQubeProject binding);
    }

    [Export(typeof(ISolutionBindingDataWriter))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class SolutionBindingDataWriter : ISolutionBindingDataWriter
    {
        private readonly ISolutionBindingFileLoader solutionBindingFileLoader;
        private readonly ISolutionBindingCredentialsLoader credentialsLoader;

        [ImportingConstructor]
        public SolutionBindingDataWriter(ICredentialStoreService credentialStoreService,
            ILogger logger)
            : this(new SolutionBindingFileLoader(logger), new SolutionBindingCredentialsLoader(credentialStoreService))
        {
        }

        internal /* for testing */ SolutionBindingDataWriter(
            ISolutionBindingFileLoader solutionBindingFileLoader,
            ISolutionBindingCredentialsLoader credentialsLoader)
        {
            this.solutionBindingFileLoader = solutionBindingFileLoader;
            this.credentialsLoader = credentialsLoader;
        }

        /// <summary>
        /// Writes the binding configuration file to the source controlled file system
        /// </summary>
        public bool Write(string configFilePath, BoundSonarQubeProject binding)
        {
            if (binding == null)
            {
                throw new ArgumentNullException(nameof(binding));
            }

            if (string.IsNullOrEmpty(configFilePath))
            {
                return false;
            }

            if (!solutionBindingFileLoader.Save(configFilePath, binding))
            {
                return false;
            }

            credentialsLoader.Save(binding.Credentials, binding.ServerUri);

            return true;
        }
    }
}
