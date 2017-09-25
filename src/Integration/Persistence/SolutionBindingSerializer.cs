/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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
using EnvDTE;
using Microsoft.Alm.Authentication;
using Newtonsoft.Json;
using SonarLint.VisualStudio.Integration.Resources;
using SonarQube.Client.Helpers;

namespace SonarLint.VisualStudio.Integration.Persistence
{
    internal class SolutionBindingSerializer : ISolutionBindingSerializer
    {
        private readonly IServiceProvider serviceProvider;
        private readonly ICredentialStore credentialStore;

        public const string SonarQubeSolutionBindingConfigurationFileName = "SolutionBinding.sqconfig";
        public const string StoreNamespace = "SonarLint.VisualStudio.Integration";

        public SolutionBindingSerializer(IServiceProvider serviceProvider)
            : this(serviceProvider, new SecretStore(StoreNamespace))
        {
        }

        internal /*for testing purposes*/ SolutionBindingSerializer(IServiceProvider serviceProvider, ICredentialStore store)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            if (store == null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            this.serviceProvider = serviceProvider;
            this.credentialStore = store;
        }

        internal ICredentialStore Store
        {
            get { return this.credentialStore; }
        }

        public BoundSonarQubeProject ReadSolutionBinding()
        {
            string configFile = this.GetSonarQubeConfigurationFilePath();
            if (string.IsNullOrWhiteSpace(configFile) || !File.Exists(configFile))
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

            ISourceControlledFileSystem sccFileSystem = this.serviceProvider.GetService<ISourceControlledFileSystem>();
            sccFileSystem.AssertLocalServiceIsNotNull();

            string configFile = this.GetSonarQubeConfigurationFilePath();
            if (string.IsNullOrWhiteSpace(configFile))
            {
                return null;
            }

            sccFileSystem.QueueFileWrite(configFile, () =>
            {
                if (this.WriteBindingInformation(configFile, binding))
                {
                    this.AddSolutionItemFile(configFile);
                    this.RemoveSolutionItemFile(configFile);
                    return true;
                }

                return false;
            });

            return configFile;
        }

        private void AddSolutionItemFile(string configFile)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(configFile), "Invalid configuration file");

            var projectSystemHelper = this.serviceProvider.GetService<IProjectSystemHelper>();
            projectSystemHelper.AssertLocalServiceIsNotNull();

            Project solutionItemsProject = projectSystemHelper.GetSolutionFolderProject(Constants.SonarQubeManagedFolderName, true);
            if (solutionItemsProject == null)
            {
                Debug.Fail("Could not find the solution items project"); // Should never happen
            }
            else
            {
                projectSystemHelper.AddFileToProject(solutionItemsProject, configFile);
            }
        }

        private void RemoveSolutionItemFile(string configFile)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(configFile), "Invalid configuration file");

            var projectSystemHelper = this.serviceProvider.GetService<IProjectSystemHelper>();
            projectSystemHelper.AssertLocalServiceIsNotNull();

            Project solutionItemsProject = projectSystemHelper.GetSolutionItemsProject(false);
            if (solutionItemsProject != null)
            {
                // Remove file from project and if project is empty, remove project from solution
                var fileName = Path.GetFileName(configFile);
                projectSystemHelper.RemoveFileFromProject(solutionItemsProject, fileName);
            }
        }

        private string GetSonarQubeConfigurationFilePath()
        {
            var solutionRuleSetsInfoProvider = this.serviceProvider.GetService<ISolutionRuleSetsInformationProvider>();
            string rootFolder = solutionRuleSetsInfoProvider.GetSolutionSonarQubeRulesFolder();

            // When the solution is closed return null
            if (rootFolder == null)
            {
                return null;
            }

            return Path.Combine(rootFolder, SonarQubeSolutionBindingConfigurationFileName);
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
            Target = "~M:SonarLint.VisualStudio.Integration.Persistence.SolutionBinding.WriteBindingInformation(System.String,SonarLint.VisualStudio.Integration.Persistence.BoundSonarQubeProject)~System.Boolean")]
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

        private static void WriteConfig(string configFile, BoundSonarQubeProject binding)
        {
            string directory = Path.GetDirectoryName(configFile);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(configFile, JsonHelper.Serialize(binding));
        }

        private static void ReadConfig(string configFile, out string text)
        {
            text = File.ReadAllText(configFile);
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
                    VsShellUtils.WriteToSonarLintOutputPane(this.serviceProvider, Strings.FailedToDeserializeSQCOnfiguration, configFilePath);
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
                VsShellUtils.WriteToSonarLintOutputPane(this.serviceProvider, e.Message);
                return false;
            }
        }
    }
}
