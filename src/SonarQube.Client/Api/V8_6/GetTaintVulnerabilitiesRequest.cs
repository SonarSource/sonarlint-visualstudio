/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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

using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using SonarQube.Client.Api.V7_20;
using SonarQube.Client.Logging;
using SonarQube.Client.Models;

namespace SonarQube.Client.Api.V8_6
{
    public class GetTaintVulnerabilitiesRequest : IGetTaintVulnerabilitiesRequest
    {
        protected GetIssuesRequest getIssuesRequest = new GetIssuesRequest();

        public string ProjectKey { get; set; }
        public string Branch { get; set; }
        public ILogger Logger { get; set; }

        public virtual async Task<SonarQubeIssue[]> InvokeAsync(HttpClient httpClient, CancellationToken token)
        {
            getIssuesRequest.Logger = Logger;
            getIssuesRequest.ProjectKey = ProjectKey;
            getIssuesRequest.Branch = Branch;
            getIssuesRequest.Statuses = "OPEN,CONFIRMED,REOPENED,RESOLVED";
            getIssuesRequest.Types = "VULNERABILITY";

            var vulnerabilities = await getIssuesRequest.InvokeAsync(httpClient, token);
            WarnForApiLimit(vulnerabilities, getIssuesRequest);

            var taintVulnerabilities = vulnerabilities
                .Where(x => x.RuleId.Contains("security"))
                .ToArray();

            return taintVulnerabilities;
        }

        private void WarnForApiLimit(SonarQubeIssue[] issues, GetIssuesRequest request)
        {
            if (issues.Length == request.ItemsLimit)
            {
                Logger.Warning($"Sonar web API response limit reached ({request.ItemsLimit} items). Some vulnerabilities might not be shown.");
            }
        }
    }
}
