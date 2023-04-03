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

using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using SonarQube.Client.Api.V5_50;
using SonarQube.Client.Logging;
using SonarQube.Client.Models;

namespace SonarQube.Client.Api.V9_5
{
    public class GetRulesWithDescriptionSectionsRequest : IGetRulesRequest
    {
        private static readonly IList<string> ResponseFieldsOverride = GetRulesRequest.ResponseList.Concat(new List<string> { "descriptionSections" }).ToList();

        private readonly GetRulesRequest innerRequest = new GetRulesRequest();

        public bool? IsActive { get; set; }
        public string QualityProfileKey { get; set; }
        public string RuleKey { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int ItemsLimit { get; set; }
        public ILogger Logger { get; set; }
        public IList<string> ResponseFieldsList { get; set; }

        public async Task<SonarQubeRule[]> InvokeAsync(HttpClient httpClient, CancellationToken token)
        {
            innerRequest.IsActive = this.IsActive;
            innerRequest.QualityProfileKey = this.QualityProfileKey;
            innerRequest.RuleKey = this.RuleKey;
            innerRequest.Logger = this.Logger;

            innerRequest.ResponseListField = ResponseFieldsOverride;

            return await innerRequest.InvokeAsync(httpClient, token);
        }
    }
}
