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
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace SonarLint.VisualStudio.E2E
{
    /// <summary>
    /// Runs a disposable loopback SonarQube through the Orchestrator launcher in <c>server/</c>.
    /// The launcher is started like the product starts SLCore: java -classpath ... MainClass.
    /// Artifact credentials reach only the launcher; the token never leaves this process.
    /// </summary>
    internal sealed class SonarQubeServer : IDisposable
    {
        private static readonly TimeSpan StartTimeout = TimeSpan.FromMinutes(10);
        private static readonly string[] CredentialVariables = { "ARTIFACTORY_USER", "ARTIFACTORY_PASSWORD" };
        private readonly string privateDirectory = Path.Combine(Path.GetTempPath(), "extension-e2e-server-" + Guid.NewGuid().ToString("N"));
        private readonly string diagnosticsDirectory;
        private readonly List<string> secrets = new List<string>();
        private readonly StringBuilder launcherOutput = new StringBuilder();
        private readonly Process launcher;
        private string logsDirectory;

        public Uri Url { get; }
        public string Token { get; }

        public SonarQubeServer(string diagnosticsDirectory)
        {
            this.diagnosticsDirectory = diagnosticsDirectory;
            var serverDirectory = Environment.GetEnvironmentVariable("E2E_SERVER_DIRECTORY")
                ?? throw new InvalidOperationException("Set E2E_SERVER_DIRECTORY to tests/e2e/server after building it with Maven.");
            var javaHome = Environment.GetEnvironmentVariable("JAVA_HOME")
                ?? throw new InvalidOperationException("Set JAVA_HOME to a JDK 21.");
            var target = Path.Combine(serverDirectory, "target");
            var classpath = Path.Combine(target, "classes") + ";" + File.ReadAllText(Path.Combine(target, "classpath.txt")).Trim();
            Directory.CreateDirectory(privateDirectory);

            var startInfo = new ProcessStartInfo(Path.Combine(javaHome, "bin", "java.exe"),
                $"-classpath {Quote(classpath)} ConnectedServer {Quote(privateDirectory)}")
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            // Hand the artifact credentials to the launcher only, then remove them from this
            // process so the IDEs started later never inherit them.
            var user = Environment.GetEnvironmentVariable("ARTIFACTORY_USER");
            var password = Environment.GetEnvironmentVariable("ARTIFACTORY_PASSWORD");
            AddSecrets(user, password, Base64($"{user}:{password}"));
            foreach (var name in CredentialVariables)
            {
                startInfo.EnvironmentVariables[name] = Environment.GetEnvironmentVariable(name);
                Environment.SetEnvironmentVariable(name, null);
            }

            launcher = new Process { StartInfo = startInfo };
            launcher.OutputDataReceived += (_, e) => Capture(e.Data);
            launcher.ErrorDataReceived += (_, e) => Capture(e.Data);
            launcher.Start();
            launcher.BeginOutputReadLine();
            launcher.BeginErrorReadLine();
            try
            {
                var info = WaitForServerInfo();
                Url = new Uri((string)info["url"]);
                Token = (string)info["token"] ?? throw new InvalidDataException("The launcher did not provide a token.");
                logsDirectory = (string)info["logs"];
                AddSecrets(Token, Base64(Token + ":"));
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private JObject WaitForServerInfo()
        {
            var infoPath = Path.Combine(privateDirectory, "server.json");
            var watch = Stopwatch.StartNew();
            while (!File.Exists(infoPath))
            {
                if (launcher.HasExited) { throw new InvalidOperationException($"The SonarQube launcher exited ({launcher.ExitCode}) before the server was ready."); }
                if (watch.Elapsed > StartTimeout) { throw new TimeoutException("SonarQube did not become ready within " + StartTimeout); }
                Thread.Sleep(2000);
            }
            return JObject.Parse(File.ReadAllText(infoPath));
        }

        public void Dispose()
        {
            try
            {
                if (!launcher.HasExited)
                {
                    // The launcher stops the server when this file appears.
                    File.WriteAllText(Path.Combine(privateDirectory, "stop"), "");
                    if (!launcher.WaitForExit(60000)) { KillProcessTree(launcher.Id); }
                }
                // The parameterless overload also flushes the asynchronous output readers.
                if (launcher.WaitForExit(30000)) { launcher.WaitForExit(); }
                ExportDiagnostics();
            }
            finally
            {
                launcher.Dispose();
                try { Directory.Delete(privateDirectory, true); }
                catch (IOException) { /* Best effort: the directory is in the runner's temp folder. */ }
            }
        }

        /// <summary>
        /// Exports launcher and server logs with every known secret redacted. Best effort:
        /// a server process can still hold a log open, and that must not fail the test.
        /// </summary>
        private void ExportDiagnostics()
        {
            Directory.CreateDirectory(diagnosticsDirectory);
            string output;
            lock (launcherOutput) { output = launcherOutput.ToString(); }
            File.WriteAllText(Path.Combine(diagnosticsDirectory, "launcher.log"), Redact(output));
            if (logsDirectory == null || !Directory.Exists(logsDirectory)) { return; }
            foreach (var name in new[] { "sonar.log", "web.log", "ce.log", "es.log" })
            {
                var path = Path.Combine(logsDirectory, name);
                if (!File.Exists(path)) { continue; }
                var exported = Path.Combine(diagnosticsDirectory, name);
                try
                {
                    // Elasticsearch may still be writing its log, so allow shared access.
                    using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                    using var reader = new StreamReader(stream);
                    File.WriteAllText(exported, Redact(reader.ReadToEnd()));
                }
                catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
                {
                    File.WriteAllText(exported + ".unavailable.txt", "Could not export the log: " + exception.Message);
                }
            }
        }

        private void Capture(string line)
        {
            if (line == null) { return; }
            lock (launcherOutput) { launcherOutput.AppendLine(line); }
        }

        private void AddSecrets(params string[] values)
        {
            foreach (var value in values)
            {
                if (!string.IsNullOrEmpty(value)) { secrets.Add(value); }
            }
        }

        private string Redact(string text)
        {
            foreach (var secret in secrets) { text = text.Replace(secret, "[REDACTED]"); }
            return text;
        }

        private static void KillProcessTree(int processId)
        {
            // Orchestrator starts the server's JVMs as child processes; Process.Kill() would orphan them.
            using var taskkill = Process.Start(new ProcessStartInfo("taskkill.exe", $"/PID {processId} /T /F")
            {
                CreateNoWindow = true,
                UseShellExecute = false
            });
            taskkill?.WaitForExit(30000);
        }

        private static string Base64(string value) => Convert.ToBase64String(Encoding.UTF8.GetBytes(value));

        private static string Quote(string value)
        {
            if (value.Contains("\"")) { throw new ArgumentException("Invalid path."); }
            return "\"" + value + "\"";
        }
    }
}
