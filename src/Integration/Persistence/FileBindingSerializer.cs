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
using SonarLint.VisualStudio.Integration.Resources;
using SonarQube.Client.Helpers;

namespace SonarLint.VisualStudio.Integration.Persistence
{
    internal abstract class FileBindingSerializer : ISolutionBindingSerializer
    {
        private readonly ICredentialStore credentialStore;
        private readonly IFile fileWrapper;

        protected readonly ISourceControlledFileSystem sccFileSystem;
        protected readonly ILogger logger;

        protected enum WriteMode
        {
            // The file will be enqued but not actually written.
            // It is the responsibility of the caller to flush the queue.
            // This option should be used if multiple other files are being
            // written using the same instance of the SCC wrapper
            // (e.g. legacy connected mode, which also writes the rulesets)
            Queued,

            // The file will be written to the SCC file system immediately
            Immediate
        }

        protected FileBindingSerializer(ISourceControlledFileSystem sccFileSystem, ICredentialStore store,
            ILogger logger, IFile fileWrapper)
        {
            if (sccFileSystem == null)
            {
                throw new ArgumentNullException(nameof(sccFileSystem));
            }
            if (store == null)
            {
                throw new ArgumentNullException(nameof(store));
            }
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }
            if (fileWrapper == null)
            {
                throw new ArgumentNullException(nameof(fileWrapper));
            }

            this.sccFileSystem = sccFileSystem;
            this.credentialStore = store;
            this.logger = logger;
            this.fileWrapper = fileWrapper;
        }

        protected abstract WriteMode Mode { get; }

        protected abstract string GetFullConfigurationFilePath();

        protected virtual bool OnSuccessfulFileWrite(string filePath)
        {
            // Default implementation is a no-op
            return true;
        }

        internal ICredentialStore Store
        {
            get { return this.credentialStore; }
        }

        public BoundSonarQubeProject ReadSolutionBinding()
        {
            string configFile = this.GetFullConfigurationFilePath();
            if (string.IsNullOrWhiteSpace(configFile) || !fileWrapper.Exists(configFile))
            {
                return null;
            }

            return this.ReadBindingInformation(configFile);
        }

        public string WriteSolutionBinding(BoundSonarQubeProject binding)
        {
            if (binding == null)
            {
                throw new ArgumentNullException(nameof(binding));
            }

            string configFile = this.GetFullConfigurationFilePath();
            if (string.IsNullOrWhiteSpace(configFile))
            {
                return null;
            }

            sccFileSystem.QueueFileWrite(configFile, () =>
            {
                if (this.WriteBindingInformation(configFile, binding))
                {
                    return OnSuccessfulFileWrite(configFile);
                }

                return false;
            });

            if (this.Mode == WriteMode.Immediate && !sccFileSystem.WriteQueuedFiles())
            {
                return null; // write operation failed
            }

            return configFile;
        }
        
        private BoundSonarQubeProject ReadBindingInformation(string configFile)
        {
            BoundSonarQubeProject bound = this.SafeDeserializeConfigFile(configFile);
            if (bound?.ServerUri != null)
            {
                var credentials = this.credentialStore.ReadCredentials(bound.ServerUri);
                if (credentials != null)
                {
                    bound.Credentials = new BasicAuthCredentials(credentials.Username,
                        credentials.Password.ToSecureString());
                }
            }

            return bound;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability",
            "S3215:\"interface\" instances should not be cast to concrete types",
            Justification = "Casting as BasicAuthCredentials is because it's the only credential type we support. Once we add more we need to think again on how to refactor the code to avoid this",
            Scope = "member",
            Target = "~M:SonarLint.VisualStudio.Integration.Persistence.FileBindingSerializer.WriteBindingInformation(System.String,SonarLint.VisualStudio.Integration.Persistence.BoundProject)~System.Boolean")]
        private bool WriteBindingInformation(string configFile, BoundSonarQubeProject binding)
        {
            if (this.SafePerformFileSystemOperation(() => WriteConfig(configFile, binding)))
            {
                BasicAuthCredentials credentials = binding.Credentials as BasicAuthCredentials;
                if (credentials != null)
                {
                    Debug.Assert(credentials.UserName != null, "User name is not expected to be null");
                    Debug.Assert(credentials.Password != null, "Password name is not expected to be null");

                    var creds = new Credential(credentials.UserName, credentials.Password.ToUnsecureString());
                    this.credentialStore.WriteCredentials(binding.ServerUri, creds);
                }
                return true;
            }

            return false;
        }

        private void WriteConfig(string configFile, BoundSonarQubeProject binding)
        {
            string directory = Path.GetDirectoryName(configFile);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            fileWrapper.WriteAllText(configFile, JsonHelper.Serialize(binding));
        }

        private void ReadConfig(string configFile, out string text)
        {
            text = fileWrapper.ReadAllText(configFile);
        }

        private BoundSonarQubeProject SafeDeserializeConfigFile(string configFilePath)
        {
            string configJson = null;
            if (this.SafePerformFileSystemOperation(() => ReadConfig(configFilePath, out configJson)))
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

        private bool SafePerformFileSystemOperation(Action operation)
        {
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
