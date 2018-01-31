/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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
using System.Diagnostics;
using System.IO;
using Microsoft.Alm.Authentication;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Integration.Helpers;
using SonarLint.VisualStudio.Integration.Persistence;

namespace SonarLint.VisualStudio.Integration.NewConnectedMode
{
    /// <summary>
    /// Reads/write the binding configuration for the new connected mode.
    /// The legacy connected mode binding is written using <see cref="SolutionBindingSerializer"/>.
    /// </summary>
    internal class ConfigurationSerializer : ISolutionBindingSerializer
    {
        private readonly IVsSolution solution;
        private readonly ICredentialStore credentialStore;
        private readonly ILogger logger;
        private readonly IFile fileWrapper;

        public ConfigurationSerializer(IVsSolution solution, ICredentialStore credentialStore, ILogger logger)
            :this(solution, credentialStore, logger, new FileWrapper())
        {

        }

        internal /* for testing */ ConfigurationSerializer(IVsSolution solution, ICredentialStore credentialStore, ILogger logger, IFile fileWrapper)
        {
            if (solution == null)
            {
                throw new ArgumentNullException(nameof(solution));
            }
            if (credentialStore == null)
            {
                throw new ArgumentNullException(nameof(credentialStore));
            }
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }
            Debug.Assert(fileWrapper != null);

            this.solution = solution;
            this.credentialStore = credentialStore;
            this.logger = logger;
            this.fileWrapper = fileWrapper;
        }

        #region ISolutionBindingSerializer interface

        public BoundSonarQubeProject ReadSolutionBinding()
        {
            var solutionFilePath = GetCurrentSolutionFilePath();
            string configFilePath = GetConnectionFilePath(solutionFilePath);
            return ConfigFileUtilities.ReadBindingFile(configFilePath, credentialStore, logger, fileWrapper);
        }

        public string WriteSolutionBinding(BoundSonarQubeProject binding)
        {
            throw new NotImplementedException();
        }

        #endregion

        internal static string GetConnectionFilePath(string solutionFilePath)
        {
            if (solutionFilePath == null)
            {
                return null;
            }

            var solutionFolder = Path.GetDirectoryName(solutionFilePath);
            var solutionName = Path.GetFileNameWithoutExtension(solutionFilePath);

            return Path.Combine(solutionFolder, ".sonarlint", $"{solutionName}.sqconfig");
        }

        private string GetCurrentSolutionFilePath()
        {
            object fullSolutionName;
            // If there isn't an open solution the returned hresult will indicate an error
            // and the returned solution name will be null. We'll just ignore the hresult.
            solution.GetProperty((int)__VSPROPID.VSPROPID_SolutionFileName, out fullSolutionName);
            return (string)fullSolutionName;
        }
    }
}