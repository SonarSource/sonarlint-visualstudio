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
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace SonarLint.VisualStudio.E2E
{
    /// <summary>
    /// Scenario-agnostic driver for one isolated Visual Studio instance with the VSIX installed.
    /// </summary>
    internal sealed class VisualStudioSession : IDisposable
    {
        private const string ExtensionId = "SonarLint.0DE19254-1DCA-4479-8EAF-58E5D677FF4D";
        private const string IdentityXPath = "/*[local-name()='PackageManifest']/*[local-name()='Metadata']/*[local-name()='Identity']";
        private const int RpcCallRejected = unchecked((int)0x80010001);
        private const int RpcServerCallRetryLater = unchecked((int)0x8001010A);
        private static readonly TimeSpan PhaseTimeout = TimeSpan.FromSeconds(180);
        private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(500);
        private readonly E2ESettings settings;
        private readonly TestContext context;
        private readonly ComMessageFilter messageFilter;
        private readonly string rootSuffix = "ExtensionE2E" + Guid.NewGuid().ToString("N").Substring(0, 8);
        private readonly Dictionary<string, object> report = new Dictionary<string, object>
        {
            ["status"] = "failed",
            ["phase"] = "prerequisites",
            ["startedAt"] = DateTime.UtcNow.ToString("o")
        };
        private Process ideProcess;
        private EnvDTE80.DTE2 dte;
        private ErrorListEntry[] lastErrors = Array.Empty<ErrorListEntry>();
        private string profileDirectory;

        public VisualStudioSession(E2ESettings settings, TestContext context)
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
            {
                throw new PlatformNotSupportedException("Requires Windows and a full Visual Studio installation.");
            }
            this.settings = settings;
            this.context = context;
            // Registers on the current thread and fails unless it is the STA thread DTE automation requires.
            messageFilter = ComMessageFilter.Register();
            Directory.CreateDirectory(settings.ResultsDirectory);
            report["rootSuffix"] = rootSuffix;
            using (var currentProcess = Process.GetCurrentProcess())
            {
                report["sessionId"] = currentProcess.SessionId;
            }
            report["userInteractive"] = Environment.UserInteractive;
            report["sessionName"] = Environment.GetEnvironmentVariable("SESSIONNAME");
        }

        /// <summary>Full path of the document opened by <see cref="OpenSolution"/>.</summary>
        public string DocumentPath { get; private set; }

        public void InstallAndLaunch()
        {
            using (var stream = File.OpenRead(settings.VsixPath))
            using (var sha = SHA256.Create())
            {
                report["vsixSha256"] = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
            }
            using (var archive = ZipFile.OpenRead(settings.VsixPath))
            using (var stream = archive.GetEntry("extension.vsixmanifest")?.Open()
                ?? throw new InvalidDataException("Missing extension.vsixmanifest."))
            {
                var manifest = LoadManifest(stream);
                if (IdentityAttribute(manifest, "Id") != ExtensionId)
                {
                    throw new InvalidDataException("The VSIX is not SonarQube for Visual Studio.");
                }
                report["vsixVersion"] = IdentityAttribute(manifest, "Version");
                File.WriteAllText(Path.Combine(settings.ResultsDirectory, "extension.vsixmanifest"), manifest.OuterXml);
            }

            var instance = DescribeInstallation();
            var ideDirectory = Path.Combine(instance.InstallationPath, "Common7", "IDE");
            var devenv = Path.Combine(ideDirectory, "devenv.exe");
            var targetingPack = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Reference Assemblies", "Microsoft", "Framework", ".NETFramework", "v4.7.2");
            if (!Directory.Exists(targetingPack))
            {
                throw new DirectoryNotFoundException("Missing .NET Framework 4.7.2 targeting pack.");
            }
            profileDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Microsoft", "VisualStudio", $"{settings.ExpectedVisualStudioVersion.Major}.0_{instance.InstanceId}{rootSuffix}");
            report["profileDirectory"] = profileDirectory;

            Phase("initialize-profile");
            ResetProfile(devenv, ideDirectory, "initialize-profile.log");
            Phase("install-vsix");
            RunProcess(Path.Combine(ideDirectory, "VSIXInstaller.exe"),
                $"/quiet /instanceIds:{instance.InstanceId} /rootSuffix:{rootSuffix} /logFile:{Quote(Path.Combine(settings.ResultsDirectory, "vsix-installer.log"))} {Quote(settings.VsixPath)}", "vsix-process.log");
            Phase("verify-vsix-installation");
            WaitUntil("the signed VSIX in the requested experimental profile", VerifyInstalledExtension);

            // Match Microsoft's integration harness: rebuild extension caches and
            // configuration before launching a newly installed experimental profile.
            Phase("configure-vsix");
            RunProcess(devenv, $"/ClearCache /RootSuffix {rootSuffix}", "clear-cache.log");
            RunProcess(devenv, $"/UpdateConfiguration /RootSuffix {rootSuffix}", "update-configuration.log");
            ResetProfile(devenv, ideDirectory, "post-install-profile.log");

            Phase("launch-ide");
            ideProcess = Process.Start(new ProcessStartInfo(devenv,
                $"/RootSuffix {rootSuffix} /NoSplash /LCID 1033 /Log {Quote(Path.Combine(settings.ResultsDirectory, "ActivityLog.xml"))}") { UseShellExecute = false });
            WaitUntil("the launched IDE's DTE object", () =>
            {
                dte = (EnvDTE80.DTE2)VisualStudioAutomation.FindDte(ideProcess.Id, settings.ExpectedVisualStudioVersion.Major);
                return dte != null;
            });
        }

        private Installation DescribeInstallation()
        {
            // The workflow discovers the IDE once; here we only verify that installation independently of the runner.
            var vswhere = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft Visual Studio", "Installer", "vswhere.exe");
            var description = RunProcess(vswhere, $"-path {Quote(settings.VisualStudioInstallPath)} -format json", "vswhere.log");
            File.WriteAllText(Path.Combine(settings.ResultsDirectory, "visual-studio-instance.json"), description);
            var instance = JsonConvert.DeserializeObject<Installation[]>(description).SingleOrDefault()
                ?? throw new InvalidOperationException($"No Visual Studio instance found at {settings.VisualStudioInstallPath}.");
            report["instanceId"] = instance.InstanceId;
            report["visualStudioProductId"] = instance.ProductId;
            report["expectedVisualStudioVersion"] = settings.ExpectedVisualStudioVersion.ToString();
            report["visualStudioVersion"] = instance.InstallationVersion;
            if (instance.ProductId != settings.ExpectedVisualStudioProduct)
            {
                throw new InvalidOperationException($"Expected IDE product {settings.ExpectedVisualStudioProduct}; found {instance.ProductId}.");
            }
            if (Version.Parse(instance.InstallationVersion) != settings.ExpectedVisualStudioVersion)
            {
                throw new InvalidOperationException($"Expected VS {settings.ExpectedVisualStudioVersion}; discovered {instance.InstallationVersion}.");
            }
            if (!File.Exists(Path.Combine(instance.InstallationPath, "Common7", "IDE", "devenv.exe")))
            {
                throw new InvalidOperationException($"{instance.InstallationPath} is not a full Visual Studio IDE.");
            }
            return instance;
        }

        private void ResetProfile(string devenv, string ideDirectory, string logName) =>
            RunProcess(devenv, $"/RootSuffix {rootSuffix} /ResetSettings {Quote(Path.Combine(ideDirectory, "Profiles", "General.vssettings"))} /Command File.Exit /NoSplash /LCID 1033", logName);

        private bool VerifyInstalledExtension()
        {
            var extensions = Path.Combine(profileDirectory, "Extensions");
            if (!Directory.Exists(extensions)) { return false; }
            foreach (var manifestPath in Directory.GetFiles(extensions, "extension.vsixmanifest", SearchOption.AllDirectories))
            {
                XmlDocument manifest;
                using (var stream = File.OpenRead(manifestPath))
                {
                    manifest = LoadManifest(stream);
                }
                if (IdentityAttribute(manifest, "Id") == ExtensionId
                    && IdentityAttribute(manifest, "Version") == (string)report["vsixVersion"])
                {
                    File.Copy(manifestPath, Path.Combine(settings.ResultsDirectory, "installed-extension.vsixmanifest"), true);
                    report["installedExtensionDirectory"] = Path.GetDirectoryName(manifestPath);
                    return true;
                }
            }
            return false;
        }

        private static XmlDocument LoadManifest(Stream stream)
        {
            var document = new XmlDocument { XmlResolver = null };
            using var reader = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
            document.Load(reader);
            return document;
        }

        private static string IdentityAttribute(XmlDocument manifest, string name) =>
            manifest.SelectSingleNode(IdentityXPath)?.Attributes?[name]?.Value;

        /// <summary>
        /// Copies the Fixture folder into the results directory (<c>*.txt</c> templates lose that extension),
        /// then opens <paramref name="solutionFile"/> and <paramref name="documentFile"/> and shows the Error List.
        /// </summary>
        public void OpenSolution(string solutionFile, string documentFile)
        {
            Phase("open-solution");
            var fixtureDirectory = Path.Combine(settings.ResultsDirectory, "fixture");
            Directory.CreateDirectory(fixtureDirectory);
            foreach (var file in Directory.GetFiles(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fixture")))
            {
                File.Copy(file, Path.Combine(fixtureDirectory, Path.GetFileName(file).Replace(".txt", "")));
            }
            DocumentPath = Path.Combine(fixtureDirectory, documentFile);
            WaitUntil("the solution to open", () =>
            {
                if (!dte.Solution.IsOpen) { dte.Solution.Open(Path.Combine(fixtureDirectory, solutionFile)); }
                return dte.Solution.IsOpen;
            });
            WaitUntil("the document to open", () =>
            {
                dte.ItemOperations.OpenFile(DocumentPath);
                return dte.ActiveDocument?.FullName == DocumentPath;
            });
            dte.ExecuteCommand("View.ErrorList");
        }

        /// <summary>Waits until a snapshot of the real Error List satisfies <paramref name="condition"/> and returns it.</summary>
        public ErrorListEntry[] WaitForErrorList(string phase, Func<ErrorListEntry[], bool> condition) =>
            WaitForErrorListStable(phase, condition, TimeSpan.Zero);

        /// <summary>
        /// Waits until every Error List snapshot satisfies <paramref name="condition"/> for <paramref name="stableFor"/>
        /// and returns the last snapshot.
        /// </summary>
        public ErrorListEntry[] WaitForErrorListStable(string phase, Func<ErrorListEntry[], bool> condition, TimeSpan stableFor)
        {
            Phase(phase);
            WaitUntil(phase, () => condition(lastErrors = VisualStudioAutomation.ReadErrorList(dte)), stableFor);
            return lastErrors;
        }

        /// <summary>Deletes a whole line (1-based) of the active document through the editor and saves it.</summary>
        public void DeleteLineAndSave(int line)
        {
            Phase("edit-document");
            var selection = (EnvDTE.TextSelection)dte.ActiveDocument.Selection;
            selection.MoveToLineAndOffset(line, 1);
            selection.MoveToLineAndOffset(line + 1, 1, true);
            selection.Delete();
            dte.ActiveDocument.Save();
        }

        public void SaveErrorList(string name, ErrorListEntry[] errors) => WriteJson(name, errors);

        public void MarkPassed()
        {
            report["status"] = "passed";
            Phase("complete");
        }

        public void RecordFailure(Exception exception) => report["error"] = exception.ToString();

        private void Phase(string phase)
        {
            report["phase"] = phase;
            context.WriteLine($"{DateTime.UtcNow:o} {phase}");
        }

        /// <summary>
        /// Polls <paramref name="condition"/> until it has held on every poll for <paramref name="stableFor"/>.
        /// A false result or a rejected COM call restarts that window. The message filter already retries
        /// busy calls for 30 s; calls still rejected after that are polled again here until the phase times out.
        /// </summary>
        private void WaitUntil(string description, Func<bool> condition, TimeSpan stableFor = default)
        {
            var watch = Stopwatch.StartNew();
            TimeSpan? satisfiedSince = null;
            COMException lastComError = null;
            while (watch.Elapsed < PhaseTimeout)
            {
                if (ideProcess != null && ideProcess.HasExited) { throw new InvalidOperationException("Visual Studio exited during " + description); }
                var satisfied = false;
                try { satisfied = condition(); }
                catch (COMException exception) when (exception.HResult == RpcCallRejected || exception.HResult == RpcServerCallRetryLater)
                {
                    lastComError = exception;
                }
                if (!satisfied)
                {
                    satisfiedSince = null;
                }
                else
                {
                    satisfiedSince ??= watch.Elapsed;
                    if (watch.Elapsed - satisfiedSince.Value >= stableFor) { return; }
                }
                Thread.Sleep(PollInterval);
            }
            throw new TimeoutException("Timed out waiting for " + description, lastComError);
        }

        private string RunProcess(string path, string arguments, string logName)
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo(path, arguments)
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };
            process.Start();
            var output = process.StandardOutput.ReadToEndAsync();
            var error = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit((int)PhaseTimeout.TotalMilliseconds))
            {
                process.Kill();
                throw new TimeoutException("Process timed out: " + path);
            }
            File.WriteAllText(Path.Combine(settings.ResultsDirectory, logName), output.GetAwaiter().GetResult() + error.GetAwaiter().GetResult());
            if (process.ExitCode != 0) { throw new InvalidOperationException($"Process failed ({process.ExitCode}): {path}"); }
            return output.GetAwaiter().GetResult();
        }

        private static string Quote(string path)
        {
            if (path.Contains("\"") || path.Contains("\r") || path.Contains("\n")) { throw new ArgumentException("Invalid path."); }
            return "\"" + path + "\"";
        }

        private void WriteJson(string name, object value) => File.WriteAllText(Path.Combine(settings.ResultsDirectory, name), JsonConvert.SerializeObject(value, Newtonsoft.Json.Formatting.Indented));

        private void CaptureProfileDiagnostics()
        {
            var diagnostics = Path.Combine(settings.ResultsDirectory, "profile-diagnostics");
            Directory.CreateDirectory(diagnostics);
            if (profileDirectory != null && Directory.Exists(profileDirectory))
            {
                var extensions = Path.Combine(profileDirectory, "Extensions");
                var files = Directory.Exists(extensions)
                    ? Directory.GetFiles(extensions, "*", SearchOption.AllDirectories)
                    : Array.Empty<string>();
                WriteJson("installed-extension-files.json", files);
                var cache = Path.Combine(profileDirectory, "ComponentModelCache");
                if (Directory.Exists(cache))
                {
                    foreach (var file in Directory.GetFiles(cache, "*.err"))
                    {
                        File.Copy(file, Path.Combine(diagnostics, Path.GetFileName(file)), true);
                    }
                }
            }
            var started = DateTime.Parse((string)report["startedAt"]).ToUniversalTime();
            foreach (var file in Directory.GetFiles(Path.GetTempPath(), "*VSIX*.log"))
            {
                if (File.GetLastWriteTimeUtc(file) >= started)
                {
                    File.Copy(file, Path.Combine(diagnostics, Path.GetFileName(file)), true);
                }
            }
        }

        public void Dispose()
        {
            try
            {
                if (dte != null)
                {
                    try { File.WriteAllText(Path.Combine(settings.ResultsDirectory, "SonarOutput.txt"), VisualStudioAutomation.ReadSonarOutput(dte)); }
                    catch (COMException exception) { context.WriteLine("Could not capture Sonar output: " + exception.Message); }
                }
                try { CaptureProfileDiagnostics(); }
                catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
                {
                    context.WriteLine("Could not capture profile diagnostics: " + exception.Message);
                }
                report["comBusyResponses"] = messageFilter.BusyCallCount;
                report["finishedAt"] = DateTime.UtcNow.ToString("o");
                WriteJson("result.json", report);
                WriteJson("last-error-list.json", lastErrors);
                foreach (var file in Directory.GetFiles(settings.ResultsDirectory)) { context.AddResultFile(file); }
            }
            finally
            {
                if (ideProcess != null)
                {
                    if (!ideProcess.HasExited) { ideProcess.Kill(); }
                    ideProcess.Dispose();
                }
                messageFilter.Dispose();
            }
        }

        public sealed class Installation
        {
            [JsonProperty("installationPath")]
            public string InstallationPath { get; set; }
            [JsonProperty("installationVersion")]
            public string InstallationVersion { get; set; }
            [JsonProperty("instanceId")]
            public string InstanceId { get; set; }
            [JsonProperty("productId")]
            public string ProductId { get; set; }
        }
    }
}
