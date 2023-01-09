﻿/*
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

using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SonarLint.VisualStudio.Integration
{
    public sealed class TelemetryClient : ITelemetryClient, IDisposable
    {
        private readonly HttpClient client;
        private readonly int maxRetries;
        private readonly TimeSpan retryTimeout;

        public TelemetryClient()
            : this(new HttpClientHandler())
        {
        }

        public TelemetryClient(HttpMessageHandler httpHandler)
            : this(httpHandler, 3, TimeSpan.FromSeconds(2))
        {
        }

        public TelemetryClient(HttpMessageHandler httpHandler, int maxRetries, TimeSpan retryTimeout)
        {
            this.maxRetries = maxRetries;
            this.retryTimeout = retryTimeout;
            this.client = new HttpClient(httpHandler)
            {
                BaseAddress = new Uri("https://telemetry.sonarsource.com", UriKind.RelativeOrAbsolute)
            };
            this.client.DefaultRequestHeaders.Add("User-Agent", "SonarLint");
        }

        public void Dispose()
        {
            this.client.Dispose();
        }

        public async Task<bool> OptOutAsync(TelemetryPayload payload)
        {
            return await RetryHelper.RetryOnExceptionAsync(maxRetries, retryTimeout,
                async () =>
                {
                    var response = await SendAsync(HttpMethod.Delete, payload);
                    response.EnsureSuccessStatusCode();
                });
        }

        public async Task<bool> SendPayloadAsync(TelemetryPayload payload)
        {
            return await RetryHelper.RetryOnExceptionAsync(maxRetries, retryTimeout,
                async () =>
                {
                    var response = await SendAsync(HttpMethod.Post, payload);
                    response.EnsureSuccessStatusCode();
                });
        }

        private async Task<HttpResponseMessage> SendAsync(HttpMethod method, TelemetryPayload payload)
        {
            var request = new HttpRequestMessage(method, "sonarlint")
            {
                Content = new StringContent(TelemetrySerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };

            return await this.client.SendAsync(request).ConfigureAwait(false);
        }
    }
}
