//-----------------------------------------------------------------------
// <copyright file="BoundSonarQubeProject.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Newtonsoft.Json;
using System;

namespace SonarLint.VisualStudio.Integration.Persistence
{
    internal class BoundSonarQubeProject 
    {
        public BoundSonarQubeProject()
        {
        }

        public BoundSonarQubeProject(Uri serverUri, string projectKey, ICredentials credentials = null)
            :this()
        {
            if (serverUri == null)
            {
                throw new ArgumentNullException(nameof(serverUri));
            }

            if (string.IsNullOrWhiteSpace(projectKey))
            {
                throw new ArgumentNullException(nameof(projectKey));
            }

            this.ServerUri = serverUri;
            this.ProjectKey = projectKey;
            this.Credentials = credentials;
        }

        public Uri ServerUri { get; set; }

        public string ProjectKey { get; set; }

        [JsonIgnore]
        public ICredentials Credentials { get; set; }
    }
}
