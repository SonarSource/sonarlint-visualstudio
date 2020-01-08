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
using System.Collections.Generic;
using Newtonsoft.Json;
using SonarQube.Client.Models;
using Language = SonarLint.VisualStudio.Core.Language;

namespace SonarLint.VisualStudio.Integration.Persistence
{
    public class BoundSonarQubeProject
    {
        public BoundSonarQubeProject()
        {
        }

        internal BoundSonarQubeProject(Uri serverUri, string projectKey, string projectName,
            ICredentials credentials = null, SonarQubeOrganization organization = null)
            : this()
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
            this.Organization = organization;
            this.ProjectKey = projectKey;
            this.ProjectName = projectName;
            this.Credentials = credentials;
        }

        public Uri ServerUri { get; set; }
        public SonarQubeOrganization Organization { get; set; }

        public string ProjectKey { get; set; }
        public string ProjectName { get; set; }
        public Dictionary<Language, ApplicableQualityProfile> Profiles { get; set; }

        [JsonIgnore]
        internal ICredentials Credentials { get; set; }
    }
}
