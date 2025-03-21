﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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

using System.Net.Http;
using SonarQube.Client.Logging;
using SonarQube.Client.Models;

namespace SonarQube.Client.Api.V7_20;

/// <summary>
/// The SonarQube 10k API result limit problem (https://github.com/SonarSource/sonarlint-visualstudio/issues/776):
/// SonarQube will return the first 10k results from any query.The suppressed issues in large
/// projects could be more than 10k and SLVS will not hide those which are not returned by the
/// server.
///
/// To reduce the effects of this limitation we will retrieve issues in batches by issue type.
/// The same approach is used in the other flavours of SonarLint.
///
/// This class should be removed if/when SonarQube removes the 10k API result limitation.
/// </summary>
internal class GetIssuesRequestWrapper<T> : IGetIssuesRequest
    where T : GetIssuesWithComponentRequest, new()
{
    private readonly T innerRequest = new T();

    public string ProjectKey { get; set; }

    public string Statuses { get; set; }

    public string Branch { get; set; }

    public string[] IssueKeys { get; set; }

    public string RuleId { get; set; }

    public string ComponentKey { get; set; }
    public string Languages { get; set; }
    public ILogger Logger { get; set; }

    public async Task<SonarQubeIssue[]> InvokeAsync(HttpClient httpClient, CancellationToken token)
    {
        // Transfer all IGetIssuesRequest properties to the inner request. If more properties are
        // added to IGetIssuesRequest, this block should set them.
        innerRequest.ProjectKey = ProjectKey;
        innerRequest.Statuses = Statuses;
        innerRequest.Branch = Branch;
        innerRequest.Logger = Logger;
        innerRequest.IssueKeys = IssueKeys;
        innerRequest.RuleId = RuleId;
        innerRequest.ComponentKey = ComponentKey;
        innerRequest.Languages = Languages;

        if (innerRequest.IssueKeys != null)
        {
            var response = await innerRequest.InvokeAsync(httpClient, token);

            return response;
        }

        ResetInnerRequest();
        innerRequest.Types = "CODE_SMELL";
        var codeSmells = await innerRequest.InvokeAsync(httpClient, token);
        WarnForApiLimit(codeSmells, innerRequest, "code smells");

        ResetInnerRequest();
        innerRequest.Types = "BUG";
        var bugs = await innerRequest.InvokeAsync(httpClient, token);
        WarnForApiLimit(bugs, innerRequest, "bugs");
        return codeSmells
            .Concat(bugs)
            .ToArray();
    }

    private void WarnForApiLimit(SonarQubeIssue[] issues, GetIssuesRequest request, string friendlyIssueType)
    {
        if (issues.Length == request.ItemsLimit)
        {
            Logger.Warning($"Sonar web API response limit reached ({request.ItemsLimit} items). Some {friendlyIssueType} might not be suppressed.");
        }
    }

    /// <summary>
    /// For paged requests the Page property is automatically changed on each invocation.
    /// We are resetting it so that our invocations for different issue types could start
    /// from the first page.
    /// </summary>
    private void ResetInnerRequest()
    {
        innerRequest.Page = 1;
    }
}
