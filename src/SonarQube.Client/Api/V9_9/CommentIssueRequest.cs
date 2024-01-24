/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SonarQube.Client.Requests;

namespace SonarQube.Client.Api.V9_9
{
    public class CommentIssueRequest : RequestBase<bool>, ICommentIssueRequest
    {
        protected override string Path => "api/issues/add_comment";
        protected override HttpMethod HttpMethod => HttpMethod.Post;
        
        [JsonProperty("issue")] 
        public string IssueKey { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        public override async Task<bool> InvokeAsync(HttpClient httpClient, CancellationToken token)
        {
            try
            {
                return await base.InvokeAsync(httpClient, token);
            }
            catch (Exception e)
            {
                Logger.Error($"{HttpMethod} {Path} request failed.");
                Logger.Debug(e.ToString());
                return false;
            }
        }
        
        protected override bool ParseResponse(string response)
        {
            // response is ignored
            return true;
        }
    }
}
