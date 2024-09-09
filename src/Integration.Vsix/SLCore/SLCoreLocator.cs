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

using System.ComponentModel.Composition;
using System.IO;
using System.IO.Abstractions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration.Vsix.Helpers;
using SonarLint.VisualStudio.SLCore.Configuration;

namespace SonarLint.VisualStudio.Integration.Vsix.SLCore
{
    [Export(typeof(ISLCoreLocator))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class SLCoreLocator : ISLCoreLocator
    {
        private const string DefaultPathInsideVsix = "Sloop\\";
        private readonly string basePathInsideVsix;
        private readonly ISonarLintSettings slSettings;
        private readonly ILogger logger;
        private readonly IFileSystem fileSystem;
        private const string SlCoreJreSubPath = "jre\\bin\\java.exe";
        private const string WindowsJreSubPath = "bin\\java.exe";
        private const string LibSubPath = "lib\\*";
        private readonly IVsixRootLocator vsixRootLocator;

        [ImportingConstructor]
        public SLCoreLocator(IVsixRootLocator vsixRootLocator, ISonarLintSettings sonarLintSettings, ILogger logger) 
            : this(vsixRootLocator, DefaultPathInsideVsix, sonarLintSettings, logger, new FileSystem())
        {
        }

        internal /* for testing */ SLCoreLocator(IVsixRootLocator vsixRootLocator,
            string basePathInsideVsix,
            ISonarLintSettings slSettings,
            ILogger logger,
            IFileSystem fileSystem)
        {
            this.vsixRootLocator = vsixRootLocator;
            this.basePathInsideVsix = basePathInsideVsix;
            this.slSettings = slSettings;
            this.logger = logger;
            this.fileSystem = fileSystem;
        }

        public SLCoreLaunchParameters LocateExecutable()
        {
            var vsixRoot = vsixRootLocator.GetVsixRoot();
            
            return new (GetCustomJrePathFromSettings() ?? GetEmbeddedJrePath(vsixRoot), 
                $"-classpath \"{Path.Combine(vsixRoot, basePathInsideVsix, LibSubPath)}\" org.sonarsource.sonarlint.core.backend.cli.SonarLintServerCli");
        }

        private string GetEmbeddedJrePath(string vsixRoot)
        {
            return Path.Combine(vsixRoot, basePathInsideVsix, SlCoreJreSubPath);
        }

        private string GetCustomJrePathFromSettings()
        {
            if (string.IsNullOrWhiteSpace(slSettings.JreLocation))
            {
                return null;
            }

            var customJrePath = Path.Combine(slSettings.JreLocation, WindowsJreSubPath);
            if (fileSystem.File.Exists(customJrePath))
            {
                logger.LogVerbose(string.Format(Resources.Strings.SlCoreLocator_UsingCustomJreLocation, customJrePath));
                return customJrePath;
            }

            logger.LogVerbose(string.Format(Resources.Strings.SlCoreLocator_CustomJreLocationNotFound, customJrePath));
            return null;
        }
    }
}
