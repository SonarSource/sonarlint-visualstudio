/*
 * SonarQube Client
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

using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SonarQube.Client.Services;

namespace SonarQube.Client.Api
{
    public abstract class PagedRequestBase<TResponseItem> : RequestBase<TResponseItem[]>, IPagedRequest<TResponseItem>
    {
        private const int FirstPage = 1;
        private const int MaximumPageSize = 500;

        [JsonProperty("p")]
        public int Page { get; set; } = FirstPage;

        [JsonProperty("ps")]
        public int PageSize { get; set; } = MaximumPageSize;

        public async override Task<TResponseItem[]> InvokeAsync(HttpClient httpClient, CancellationToken token)
        {
            var result = new List<TResponseItem>();

            Result<TResponseItem[]> pageResult;
            do
            {
                pageResult = await InvokeImplAsync(httpClient, token);
                pageResult.EnsureSuccess();

                result.AddRange(pageResult.Value);

                Page++;
            }
            while (pageResult.Value.Length >= MaximumPageSize);

            return result.ToArray();
        }
    }
}
