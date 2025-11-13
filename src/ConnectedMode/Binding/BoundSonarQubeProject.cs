/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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

using Newtonsoft.Json;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Core.Binding
{
    public class BoundSonarQubeProject
    {
        public BoundSonarQubeProject()
        {
        }

        public BoundSonarQubeProject(
            Uri serverUri,
            string projectKey,
            string projectName,
            IConnectionCredentials credentials = null,
            SonarQubeOrganization organization = null)
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

            ServerUri = serverUri;
            Organization = organization;
            ProjectKey = projectKey;
            ProjectName = projectName;
            Credentials = credentials;
        }

        public Uri ServerUri { get; set; }
        public SonarQubeOrganization Organization { get; set; }

        public string ProjectKey { get; set; }
        public string ProjectName { get; set; }

        [JsonIgnore]
        public IConnectionCredentials Credentials { get; set; }
    }
}
