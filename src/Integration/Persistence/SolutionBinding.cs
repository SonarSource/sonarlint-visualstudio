//-----------------------------------------------------------------------
// <copyright file="SolutionBinding.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using EnvDTE;
using Microsoft.Alm.Authentication;
using SonarLint.VisualStudio.Integration.Resources;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;

namespace SonarLint.VisualStudio.Integration.Persistence
{
    internal class SolutionBinding
    {
        private readonly IServiceProvider serviceProvider;
        private readonly ICredentialStore credentialStore;
        private readonly IProjectSystemHelper projectSystemHelper;

        public const string SonarQubeSolutionBindingConfigurationFileName = "SolutionBinding.sqconfig";
        public const string StoreNamespace = "SonarLint.VisualStudio.Integration";

        public SolutionBinding(IServiceProvider serviceProvider, ICredentialStore credentialStore = null, IProjectSystemHelper projectSystemHelper = null)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            this.serviceProvider = serviceProvider;
            this.credentialStore = credentialStore ?? new SecretStore(StoreNamespace);
            this.projectSystemHelper = projectSystemHelper ?? new ProjectSystemHelper(this.serviceProvider);
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
            string configFile = this.GetSonarQubeConfigurationFilePath();
            if (string.IsNullOrWhiteSpace(configFile))
            {
                return null;
            }

            if (this.WriteBindingInformation(configFile, binding))
            {
                this.AddSolutionItemFile(configFile);

                return configFile;
            }

            return null;
        }

        private void AddSolutionItemFile(string configFile)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(configFile), "Invalid config file");

            Project solutionItemsProject = this.projectSystemHelper.GetSolutionItemsProject();
            if (solutionItemsProject == null)
            {
                Debug.Fail("Could not find the solution items project");
            }
            else
            {
                this.projectSystemHelper.AddFileToProject(solutionItemsProject, configFile);
            }
        }

        private string GetSonarQubeConfigurationFilePath()
        {
            var dte = this.serviceProvider.GetService(typeof(DTE)) as DTE;
            string solutionFullFilePath = dte?.Solution?.FullName;

            if (string.IsNullOrWhiteSpace(solutionFullFilePath))
            {
                return null;
            }

            return Path.Combine(Path.GetDirectoryName(solutionFullFilePath), Constants.SonarQubeManagedFolderName, SonarQubeSolutionBindingConfigurationFileName);
        }

        private BoundSonarQubeProject ReadBindingInformation(string configFile)
        {
            BoundSonarQubeProject bound = this.SafeDeserializeConfigFile(configFile);
            if (bound != null)
            {

                Credential creds;
                if (bound?.ServerUri != null && this.credentialStore.ReadCredentials(bound.ServerUri, out creds))
                {
                    bound.Credentials = new BasicAuthCredentials(creds.Username, creds.Password);
                }
            }

            return bound;
        }

        private bool WriteBindingInformation(string configFile, BoundSonarQubeProject binding)
        {
            if (this.SafePerformFileSystemOperation(() => WriteConfig(configFile, binding)))
            {
                BasicAuthCredentials credentials = binding.Credentials as BasicAuthCredentials;
                if (credentials != null)
                {
                    Debug.Assert(credentials.UserName != null, "User name is not expected to be null");
                    Debug.Assert(credentials.Password != null, "Password name is not expected to be null");

                    var creds = new Credential(credentials.UserName, credentials.Password);
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
                    VsShellUtils.WriteToGeneralOutputPane(this.serviceProvider, Strings.FailedToDeserializeSQCOnfiguration, configFilePath);
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
                VsShellUtils.WriteToGeneralOutputPane(this.serviceProvider, e.Message);
                return false;
            }
        }
    }
}
