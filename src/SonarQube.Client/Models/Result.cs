/*
 * SonarQube Client
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

using System.Net;
using System.Net.Http;

namespace SonarQube.Client.Models
{
    public struct Result<TValue>
    {
        private HttpResponseMessage response;

        public TValue Value { get; }
        public HttpStatusCode StatusCode { get; }
        public bool IsSuccess { get; }

        public Result(HttpResponseMessage response, TValue value)
        {
            this.response = response;
            IsSuccess = response.IsSuccessStatusCode;
            StatusCode = response.StatusCode;
            Value = value;
        }

        public void EnsureSuccess()
        {
            response.EnsureSuccessStatusCode();
        }
    }
}
