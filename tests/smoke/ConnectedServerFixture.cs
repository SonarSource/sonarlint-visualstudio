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
using Microsoft.Alm.Authentication;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SonarLint.VisualStudio.Smoke
{
    // Fixtures use public server APIs and the extension's on-disk/credential formats.
    // They never inject rules or call the product's analysis backend.
    internal sealed class ConnectedServerFixture : IDisposable
    {
        public const string RuleKey = "csharpsquid:S3776";
        public const int Threshold = 1;
        public const string ExpectedFinding = "Refactor this method to reduce its Cognitive Complexity from 3 to the 1 allowed.";
        private readonly string projectKey = "slvs-connected-" + Guid.NewGuid().ToString("N");
        private readonly HttpClient http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        private readonly SecretStore credentials = new SecretStore("SonarLint.VisualStudio.Integration");
        private readonly Uri server;
        private readonly string token;
        private readonly string results;
        private readonly string connectionsPath;
        private readonly string bindingDirectory;
        private string profileKey;
        private bool credentialWritten;
        private bool connectionsWritten;
        private bool bindingWritten;
        private bool projectCreated;

        public ConnectedServerFixture(string solutionPath, string resultsDirectory)
        {
            results = resultsDirectory;
            var infoPath = Environment.GetEnvironmentVariable("SMOKE_SERVER_INFO")
                ?? throw new InvalidOperationException("SMOKE_SERVER_INFO is required.");
            var info = JObject.Parse(File.ReadAllText(infoPath));
            server = new Uri((string)info["url"]);
            if (!server.IsLoopback || server.Scheme != "http")
            {
                throw new InvalidOperationException("This PoC accepts only its disposable loopback SonarQube server.");
            }
            http.BaseAddress = server;
            token = (string)info["token"] ?? throw new InvalidDataException("Missing server token.");
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(Encoding.UTF8.GetBytes(token + ":")));
            var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SonarLint for Visual Studio");
            connectionsPath = Path.Combine(root, "connections.json");
            bindingDirectory = Path.Combine(root, "Bindings", Path.GetFileNameWithoutExtension(solutionPath));
            // Refuse to overwrite another user's connections; use only a fresh CI account.
            if (File.Exists(connectionsPath) || Directory.Exists(bindingDirectory))
            {
                throw new InvalidOperationException("Connected smoke requires an unused connection store and unique solution name.");
            }
            try
            {
                var status = Get("api/system/status");
                if ((string)status["version"] != "25.11.0.114957" || (string)status["status"] != "UP")
                {
                    throw new InvalidDataException("Unexpected disposable server version/status.");
                }
                Post("api/projects/create", "project", projectKey, "name", projectKey);
                projectCreated = true;
                var profile = Post("api/qualityprofiles/create", "language", "cs", "name", projectKey);
                profileKey = (string)profile["profile"]["key"];
                var rule = Get("api/rules/show?key=" + Uri.EscapeDataString(RuleKey));
                var defaultThreshold = rule["rule"]["params"].Values<JObject>()
                    .Single(x => (string)x["key"] == "threshold")["defaultValue"];
                if ((string)defaultThreshold != "15") { throw new InvalidDataException("Unexpected default cognitive-complexity threshold."); }
                File.WriteAllText(Path.Combine(results, "server-rule-defaults.json"), rule.ToString());
                Post("api/qualityprofiles/activate_rule", "key", profileKey, "rule", RuleKey, "params", "threshold=" + Threshold);
                Post("api/qualityprofiles/add_project", "qualityProfile", projectKey, "language", "cs", "project", projectKey);
                var rules = Get("api/rules/search?activation=true&qprofile=" + Uri.EscapeDataString(profileKey) + "&ps=500");
                if ((int)rules["total"] != 1 || (string)rules["rules"][0]["key"] != RuleKey)
                {
                    throw new InvalidDataException("Server profile must contain exactly S3776; S1481 must be disabled.");
                }
                File.WriteAllText(Path.Combine(results, "server-profile.json"), rules.ToString());
                if (credentials.ReadCredentials(new TargetUri(server)) != null)
                {
                    throw new InvalidOperationException("Refusing to replace an existing server credential.");
                }
                credentials.WriteCredentials(new TargetUri(server), new Credential("PersonalAccessToken", token));
                credentialWritten = true;
                Directory.CreateDirectory(root);
                var connection = new JObject { ["serverConnections"] = new JArray(new JObject
                {
                    ["id"] = server.AbsoluteUri, ["serverUri"] = server.AbsoluteUri,
                    ["settings"] = new JObject { ["IsSmartNotificationsEnabled"] = false }
                }) };
                using (var output = new FileStream(connectionsPath, FileMode.CreateNew, FileAccess.Write))
                using (var writer = new StreamWriter(output)) { writer.Write(connection.ToString()); }
                connectionsWritten = true;
                Directory.CreateDirectory(bindingDirectory);
                bindingWritten = true;
                File.WriteAllText(Path.Combine(bindingDirectory, "binding.config"), new JObject
                {
                    ["ServerConnectionId"] = server.AbsoluteUri, ["ProjectKey"] = projectKey
                }.ToString());
            }
            catch
            {
                try { Dispose(); }
                catch (Exception cleanup) { File.WriteAllText(Path.Combine(results, "fixture-cleanup-error.txt"), cleanup.ToString()); }
                throw;
            }
        }

        public void SaveEvidence() => File.WriteAllText(Path.Combine(results, "connected-fixture.json"),
            JsonConvert.SerializeObject(new
            {
                server = server.AbsoluteUri, projectKey, profileKey, rule = RuleKey, threshold = Threshold,
                bindingSetup = "persisted connection/binding and Windows Credential Manager; binding UI not exercised"
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

        public void Dispose()
        {
            try
            {
                if (bindingWritten && Directory.Exists(bindingDirectory)) { Directory.Delete(bindingDirectory, true); }
                if (connectionsWritten && File.Exists(connectionsPath))
                {
                    var model = JObject.Parse(File.ReadAllText(connectionsPath));
                    var rows = (JArray)model["serverConnections"];
                    for (var i = rows.Count - 1; i >= 0; i--) { if ((string)rows[i]["id"] == server.AbsoluteUri) { rows.RemoveAt(i); } }
                    if (rows.Count == 0) { File.Delete(connectionsPath); }
                    else { File.WriteAllText(connectionsPath, model.ToString()); }
                }
                if (credentialWritten) { credentials.DeleteCredentials(new TargetUri(server)); }
            }
            finally
            {
                try { if (projectCreated) { Post("api/projects/delete", "project", projectKey); } }
                finally
                {
                    try { if (profileKey != null) { Post("api/qualityprofiles/delete", "qualityProfile", projectKey, "language", "cs"); } }
                    finally { http.Dispose(); }
                }
            }
        }
    }
}
