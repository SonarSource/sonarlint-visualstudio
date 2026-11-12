/*
 * SonarLint for Visual Studio
 * Copyright (C) SonarSource Sàrl
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
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SonarLint.VisualStudio.E2E
{
    /// <summary>
    /// Arranges the server side through its public Web API: a project whose C# quality profile
    /// activates only S3776 with threshold 1. The connection and binding are created by the
    /// extension itself, through its E2E commands.
    /// </summary>
    internal sealed class ConnectedServerFixture : IDisposable
    {
        public const string RuleKey = "csharpsquid:S3776";
        public const int Threshold = 1;
        public const string ExpectedFinding = "Refactor this method to reduce its Cognitive Complexity from 3 to the 1 allowed.";
        private readonly HttpClient http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        private readonly string token;
        private readonly string results;
        private readonly string profileKey;

        public ConnectedServerFixture(SonarQubeServer sonarQube, string resultsDirectory)
        {
            results = resultsDirectory;
            if (!sonarQube.Url.IsLoopback || sonarQube.Url.Scheme != "http")
            {
                throw new InvalidOperationException("The connected-mode E2E test accepts only its disposable loopback SonarQube server.");
            }
            http.BaseAddress = sonarQube.Url;
            token = sonarQube.Token;
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(Encoding.UTF8.GetBytes(token + ":")));
            try
            {
                Post("api/projects/create", "project", ProjectKey, "name", ProjectKey);
                var profile = Post("api/qualityprofiles/create", "language", "cs", "name", ProjectKey);
                profileKey = (string)profile["profile"]["key"];
                Post("api/qualityprofiles/activate_rule", "key", profileKey, "rule", RuleKey, "params", "threshold=" + Threshold);
                Post("api/qualityprofiles/add_project", "qualityProfile", ProjectKey, "language", "cs", "project", ProjectKey);
                var rules = Get("api/rules/search?activation=true&qprofile=" + Uri.EscapeDataString(profileKey) + "&ps=500");
                if ((int)rules["total"] != 1 || (string)rules["rules"][0]["key"] != RuleKey)
                {
                    throw new InvalidDataException("Server profile must contain exactly S3776; S1481 must be disabled.");
                }
                File.WriteAllText(Path.Combine(results, "server-profile.json"), rules.ToString());
            }
            catch
            {
                http.Dispose();
                throw;
            }
        }

        public string ProjectKey { get; } = "slvs-connected-" + Guid.NewGuid().ToString("N");

        public void SaveEvidence() => File.WriteAllText(Path.Combine(results, "connected-fixture.json"),
            JsonConvert.SerializeObject(new
            {
                server = http.BaseAddress.AbsoluteUri, projectKey = ProjectKey, profileKey, rule = RuleKey, threshold = Threshold,
                bindingSetup = "connection and binding created by the extension's E2E commands; binding UI not exercised"
            }, Formatting.Indented));

        private JObject Get(string path) => Request(new HttpRequestMessage(HttpMethod.Get, path));

        private JObject Post(string path, params string[] values)
        {
            var pairs = new List<KeyValuePair<string, string>>();
            for (var i = 0; i < values.Length; i += 2) { pairs.Add(new KeyValuePair<string, string>(values[i], values[i + 1])); }
            return Request(new HttpRequestMessage(HttpMethod.Post, path) { Content = new FormUrlEncodedContent(pairs) });
        }

        private JObject Request(HttpRequestMessage request)
        {
            using (request)
            using (var response = http.SendAsync(request).GetAwaiter().GetResult())
            {
                var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                if (!response.IsSuccessStatusCode)
                {
                    var detail = "";
                    try
                    {
                        detail = string.Join("; ", JObject.Parse(body)["errors"]?.Values<JObject>()
                            .Select(x => (string)x["msg"]) ?? Array.Empty<string>());
                    }
                    catch (JsonException) { /* Do not echo arbitrary non-JSON response bodies. */ }
                    detail = detail.Replace(token, "[REDACTED]")
                        .Replace(Convert.ToBase64String(Encoding.UTF8.GetBytes(token + ":")), "[REDACTED]");
                    throw new InvalidOperationException("Fixture API failed: " + request.RequestUri.AbsolutePath
                        + " HTTP " + (int)response.StatusCode + " " + detail);
                }
                return string.IsNullOrWhiteSpace(body) ? new JObject() : JObject.Parse(body);
            }
        }

        public void Dispose() => http.Dispose();
    }
}
