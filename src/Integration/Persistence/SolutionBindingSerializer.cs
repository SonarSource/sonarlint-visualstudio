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
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;
using SonarLint.VisualStudio.Core.SystemAbstractions;
using SonarLint.VisualStudio.Integration.Resources;
using SonarQube.Client.Helpers;

namespace SonarLint.VisualStudio.Integration.Persistence
{
    internal class SolutionBindingSerializer : ISolutionBindingSerializer
    {
        private readonly ILogger logger;
        private readonly IFile fileWrapper;
        private readonly IDirectory directoryWrapper;

        public SolutionBindingSerializer(ILogger logger, IFile fileWrapper, IDirectory directoryWrapper)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.fileWrapper = fileWrapper ?? throw new ArgumentNullException(nameof(fileWrapper));
            this.directoryWrapper = directoryWrapper ?? throw new ArgumentNullException(nameof(directoryWrapper));
        }

        public bool SerializeToFile(string filePath, BoundSonarQubeProject project)
        {
            var serializedProject = Serialize(project);

            return SafePerformFileSystemOperation(() => WriteConfig(filePath, serializedProject));
        }

        private void WriteConfig(string configFile, string serializedProject)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(configFile));

            var directoryName = Path.GetDirectoryName(configFile);

            if (!directoryWrapper.Exists(directoryName))
            {
                directoryWrapper.Create(directoryName);
            }

            fileWrapper.WriteAllText(configFile, serializedProject);
        }

        public BoundSonarQubeProject DeserializeFromFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !fileWrapper.Exists(filePath))
            {
                return null;
            }

            string configJson = null;

            if (SafePerformFileSystemOperation(() => ReadConfig(filePath, out configJson)))
            {
                try
                {
                    return Deserialize(configJson);
                }
                catch (JsonException)
                {
                    logger.WriteLine(Strings.FailedToDeserializeSQCOnfiguration, filePath);
                }
            }

            return null;
        }

        private void ReadConfig(string configFile, out string text)
        {
            text = fileWrapper.ReadAllText(configFile);
        }

        private bool SafePerformFileSystemOperation(Action operation)
        {
            Debug.Assert(operation != null);

            try
            {
                operation();
                return true;
            }
            catch (Exception e) when (!Microsoft.VisualStudio.ErrorHandler.IsCriticalException(e))
            {
                logger.WriteLine(e.Message);
                return false;
            }
        }

        private BoundSonarQubeProject Deserialize(string projectJson)
        {
            return JsonHelper.Deserialize<BoundSonarQubeProject>(projectJson);
        }

        private string Serialize(BoundSonarQubeProject project)
        {
            return JsonHelper.Serialize(project);
        }
    }
}
