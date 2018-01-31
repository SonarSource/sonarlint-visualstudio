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
using Newtonsoft.Json;
using SonarLint.VisualStudio.Integration.Helpers;
using SonarLint.VisualStudio.Integration.Persistence;
using SonarLint.VisualStudio.Integration.Resources;
using SonarQube.Client.Helpers;

namespace SonarLint.VisualStudio.Integration.NewConnectedMode
{
    internal static class ConfigFileUtilities
    {
        public static BoundSonarQubeProject ReadBindingFile(string fullConfigFilePath, ICredentialStore credentialStore, ILogger logger, IFile fileWrapper)
        {
            Debug.Assert(credentialStore != null);
            Debug.Assert(logger != null);

            if (string.IsNullOrWhiteSpace(fullConfigFilePath) || !fileWrapper.Exists(fullConfigFilePath))
            {
                return null;
            }

            BoundSonarQubeProject bound = SafeDeserializeConfigFile(fullConfigFilePath, logger, fileWrapper);
            if (bound?.ServerUri != null)
            {
                var credentials = credentialStore.ReadCredentials(bound.ServerUri);
                if (credentials != null)
                {
                    bound.Credentials = new BasicAuthCredentials(credentials.Username,
                        credentials.Password.ToSecureString());
                }
            }

            return bound;
        }
        
        private static BoundSonarQubeProject SafeDeserializeConfigFile(string configFilePath, ILogger logger, IFile fileWrapper)
        {
            string configJson = null;
            if (SafePerformFileSystemOperation(logger, () => { configJson = fileWrapper.ReadAllText(configFilePath); } ))
            {
                try
                {
                    return JsonHelper.Deserialize<BoundSonarQubeProject>(configJson);
                }
                catch (JsonException)
                {
                    logger.WriteLine(Strings.FailedToDeserializeSQCOnfiguration, configFilePath);
                }
            }
            return null;
        }

        public static bool SafePerformFileSystemOperation(ILogger logger, Action operation)
        {
            Debug.Assert(logger != null);
            Debug.Assert(operation != null);

            try
            {
                operation();
                return true;
            }
            catch (Exception e) when (e is PathTooLongException
                                    || e is UnauthorizedAccessException
                                    || e is FileNotFoundException
                                    || e is DirectoryNotFoundException
                                    || e is IOException
                                    || e is System.Security.SecurityException)
            {
                logger.WriteLine(e.Message);
                return false;
            }
        }
    }
}