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
    internal class ConfigurationSerializer : FileBindingSerializer
    {
        private readonly IVsSolution solution;
        private readonly IDirectory directoryWrapper;

        public ConfigurationSerializer(
            IVsSolution solution,
            ISourceControlledFileSystem sccFileSystem,
            ICredentialStore store,
            ILogger logger)
            :this(solution, sccFileSystem, store, logger, new FileWrapper(), new DirectoryWrapper())
        {
        }

        internal /* for testing */ ConfigurationSerializer(
            IVsSolution solution,
            ISourceControlledFileSystem sccFileSystem,
            ICredentialStore store,
            ILogger logger,
            IFile fileWrapper,
            IDirectory directoryWrapper)
            :base(sccFileSystem, store, logger, fileWrapper)
        {
            if (solution == null)
            {
                throw new ArgumentNullException(nameof(solution));
            }
            Debug.Assert(directoryWrapper != null);

            this.solution = solution;
            this.directoryWrapper = directoryWrapper;
        }

        public override void DeleteBinding()
        {
            //TODO: we are assuming the files are not under source control
            string configFile = this.GetFullConfigurationFilePath();

            if (!fileWrapper.Exists(configFile))
            {
                Debug.Fail($"Nothing to delete - binding file does not exist: {configFile}");
                return;
            }

            SafePerformFileSystemOperation(() =>
            {
                fileWrapper.Delete(configFile);
                string sonarLintDir = Path.GetDirectoryName(configFile);
                directoryWrapper.Delete(sonarLintDir);
            });
        }

        protected override WriteMode Mode
        {
            get { return WriteMode.Immediate; }
        }

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

        protected override string GetFullConfigurationFilePath()
        {
            object fullSolutionName;
            // If there isn't an open solution the returned hresult will indicate an error
            // and the returned solution name will be null. We'll just ignore the hresult.
            solution.GetProperty((int)__VSPROPID.VSPROPID_SolutionFileName, out fullSolutionName);

            return GetConnectionFilePath(fullSolutionName as string);
        }
    }
}