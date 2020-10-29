/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
using SonarLint.VisualStudio.Core.Binding;

namespace SonarLint.VisualStudio.Integration.Persistence
{
    internal interface ISolutionBindingDataWriter
    {
        /// <summary>
        /// Writes the binding information
        /// </summary>
        /// <returns>Has file been saved</returns>
        bool Write(string configFilePath, BoundSonarQubeProject binding, Action<string> onSuccessfulFileWrite);
    }

    internal class SolutionBindingDataWriter : ISolutionBindingDataWriter
    {
        private readonly ISourceControlledFileSystem sccFileSystem;
        private readonly ISolutionBindingFileLoader solutionBindingFileLoader;
        private readonly ISolutionBindingCredentialsLoader credentialsLoader;

        public SolutionBindingDataWriter(ISourceControlledFileSystem sccFileSystem,
            ISolutionBindingFileLoader solutionBindingFileLoader,
            ISolutionBindingCredentialsLoader credentialsLoader)
        {
            this.sccFileSystem = sccFileSystem ?? throw new ArgumentNullException(nameof(sccFileSystem));
            this.solutionBindingFileLoader = solutionBindingFileLoader ?? throw new ArgumentNullException(nameof(solutionBindingFileLoader));
            this.credentialsLoader = credentialsLoader ?? throw new ArgumentNullException(nameof(credentialsLoader));
        }

        /// <summary>
        /// Writes the binding configuration file to the source controlled file system
        /// </summary>
        /// <remarks>
        /// The file will be enqueued but not actually written.
        /// It is the responsibility of the caller to flush the queue.
        /// This is to allow multiple other files to be written using the 
        /// same instance of the SCC wrapper (e.g. ruleset files).
        /// </remarks>
        public bool Write(string configFilePath, BoundSonarQubeProject binding, Action<string> onSuccessfulFileWrite)
        {
            if (binding == null)
            {
                throw new ArgumentNullException(nameof(binding));
            }

            if (string.IsNullOrEmpty(configFilePath))
            {
                return false;
            }

            sccFileSystem.QueueFileWrite(configFilePath, () =>
            {
                if (!solutionBindingFileLoader.Save(configFilePath, binding))
                {
                    return false;
                }

                credentialsLoader.Save(binding.Credentials, binding.ServerUri);
                onSuccessfulFileWrite?.Invoke(configFilePath);

                return true;
            });

            return true;
        }
    }
}
