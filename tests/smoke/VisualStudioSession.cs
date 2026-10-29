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

namespace SonarLint.VisualStudio.Smoke
{
    internal sealed class VisualStudioSession : IDisposable
    {
        private const string FindingMessage = "Remove the unused local variable 'smokeUnused'.";
        private static readonly TimeSpan PhaseTimeout = TimeSpan.FromSeconds(180);
        private readonly string vsixPath;
        private readonly string resultsDirectory;
        private readonly TestContext context;
        private readonly string rootSuffix = "SLVS3097" + Guid.NewGuid().ToString("N").Substring(0, 8);
        private readonly Dictionary<string, object> report = new Dictionary<string, object>
        {
            ["status"] = "failed",
            ["phase"] = "prerequisites",
            ["startedAt"] = DateTime.UtcNow.ToString("o")
        };
        private Process ideProcess;
        private EnvDTE80.DTE2 dte;
        private ErrorListEntry[] lastErrors = Array.Empty<ErrorListEntry>();
        private string sourcePath;

        public VisualStudioSession(string vsixPath, string resultsDirectory, TestContext context)
        {
            this.vsixPath = Path.GetFullPath(vsixPath ?? throw new ArgumentException("Set SMOKE_VSIX_PATH to a built VSIX."));
            this.resultsDirectory = Path.GetFullPath(resultsDirectory ?? throw new ArgumentException("Set SMOKE_RESULTS_DIRECTORY to a new directory."));
            this.context = context;
            if (Directory.Exists(this.resultsDirectory))
            {
                throw new ArgumentException("Use a new results directory for each run.");
            }
            Directory.CreateDirectory(this.resultsDirectory);
            report["rootSuffix"] = rootSuffix;
        }

        public void InstallAndLaunch()
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
            {
                throw new PlatformNotSupportedException("Requires Windows and full Visual Studio 2022.");
            }
            if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
            {
                throw new InvalidOperationException("DTE automation requires an STA test thread.");
            }
            if (!string.Equals(Path.GetExtension(vsixPath), ".vsix", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("The input must be a built .vsix package.");
            }
            using (var stream = File.OpenRead(vsixPath))
            using (var sha = SHA256.Create())
            {
                report["vsixSha256"] = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
            }
            using (var archive = ZipFile.OpenRead(vsixPath))
            using (var stream = archive.GetEntry("extension.vsixmanifest")?.Open()
                ?? throw new InvalidDataException("Missing extension.vsixmanifest."))
            {
                var manifest = new XmlDocument { XmlResolver = null };
                using var reader = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
                manifest.Load(reader);
                var identity = manifest.SelectSingleNode("/*[local-name()='PackageManifest']/*[local-name()='Metadata']/*[local-name()='Identity']");
                if (identity?.Attributes?["Id"]?.Value != "SonarLint.0DE19254-1DCA-4479-8EAF-58E5D677FF4D")
                {
                    throw new InvalidDataException("The VSIX is not SonarQube for Visual Studio.");
                }
                report["vsixVersion"] = identity.Attributes["Version"].Value;
            }

            var vswhere = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft Visual Studio", "Installer", "vswhere.exe");
            var inventory = RunProcess(vswhere, "-all -products * -format json", "vswhere.log");
            File.WriteAllText(Path.Combine(resultsDirectory, "visual-studio-inventory.json"), inventory);
            var instances = JsonConvert.DeserializeObject<Installation[]>(inventory)
                .Where(x => x.InstallationVersion.StartsWith("17.", StringComparison.Ordinal)
                    && File.Exists(Path.Combine(x.InstallationPath, "Common7", "IDE", "devenv.exe"))).ToArray();
            if (instances.Length != 1)
            {
                throw new InvalidOperationException($"Expected exactly one full Visual Studio 2022 IDE; found {instances.Length}.");
            }
            var instance = instances[0];
            var ideDirectory = Path.Combine(instance.InstallationPath, "Common7", "IDE");
            var targetingPack = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Reference Assemblies", "Microsoft", "Framework", ".NETFramework", "v4.7.2");
            if (!Directory.Exists(targetingPack))
            {
                throw new DirectoryNotFoundException("Missing .NET Framework 4.7.2 targeting pack.");
            }
            report["visualStudioVersion"] = instance.InstallationVersion;
            report["instanceId"] = instance.InstanceId;
            var devenv = Path.Combine(ideDirectory, "devenv.exe");
            Phase("initialize-profile");
            RunProcess(devenv, $"/RootSuffix {rootSuffix} /ResetSettings {Quote(Path.Combine(ideDirectory, "Profiles", "General.vssettings"))} /Command File.Exit /NoSplash /LCID 1033", "initialize-profile.log");
            Phase("install-vsix");
            RunProcess(Path.Combine(ideDirectory, "VSIXInstaller.exe"),
                $"/quiet /instanceIds:{instance.InstanceId} /rootSuffix:{rootSuffix} /logFile:{Quote(Path.Combine(resultsDirectory, "vsix-installer.log"))} {Quote(vsixPath)}", "vsix-process.log");

            Phase("launch-ide");
            ideProcess = Process.Start(new ProcessStartInfo(devenv,
                $"/RootSuffix {rootSuffix} /NoSplash /LCID 1033 /Log {Quote(Path.Combine(resultsDirectory, "ActivityLog.xml"))}") { UseShellExecute = false });
            WaitUntil("the launched IDE's DTE object", () =>
            {
                dte = (EnvDTE80.DTE2)VisualStudioAutomation.FindDte(ideProcess.Id);
                return dte != null;
            });
        }

        public void OpenSample()
        {
            Phase("open-sample");
            var fixtureDirectory = Path.Combine(resultsDirectory, "fixture");
            Directory.CreateDirectory(fixtureDirectory);
            foreach (var file in Directory.GetFiles(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fixture")))
            {
                File.Copy(file, Path.Combine(fixtureDirectory, Path.GetFileName(file).Replace(".txt", "")));
            }
            sourcePath = Path.Combine(fixtureDirectory, "Smoke.cs");
            WaitUntil("the solution to open", () =>
            {
                if (!dte.Solution.IsOpen) { dte.Solution.Open(Path.Combine(fixtureDirectory, "Smoke.sln")); }
                return dte.Solution.IsOpen;
            });
            WaitUntil("the sample file to open", () =>
            {
                dte.ItemOperations.OpenFile(sourcePath);
                return dte.ActiveDocument?.FullName == sourcePath;
            });
            dte.ExecuteCommand("View.ErrorList");
        }

        public ErrorListEntry WaitForFinding()
        {
            Phase("detect-finding");
            WaitUntil("Sonar S1481 in the real Error List", () => MatchingFindings().Length > 0);
            var findings = MatchingFindings();
            report["finding"] = findings;
            return findings[0];
        }

        public ErrorListEntry[] MatchingFindings()
        {
            lastErrors = VisualStudioAutomation.ReadErrorList(dte);
            return lastErrors.Where(x => string.Equals(x.File, sourcePath, StringComparison.OrdinalIgnoreCase)
                && x.Line == 5 && x.Description == FindingMessage).ToArray();
        }

        public void RemoveUnusedVariableAndSave()
        {
            Phase("fix-file");
            var selection = (EnvDTE.TextSelection)dte.ActiveDocument.Selection;
            selection.SelectAll();
            selection.Insert(File.ReadAllText(sourcePath).Replace("        int smokeUnused = 42;", ""));
            dte.ActiveDocument.Save();
            Assert.IsFalse(File.ReadAllText(sourcePath).Contains("int smokeUnused = 42;"), "The editor must save the fixed sample to disk.");
        }

        public void WaitForFindingToDisappear(TimeSpan stableFor)
        {
            Phase("clear-finding");
            var clearSince = new Stopwatch();
            WaitUntil("the finding to remain absent after saving", () =>
            {
                if (MatchingFindings().Length > 0) { clearSince.Reset(); return false; }
                clearSince.Start();
                return clearSince.Elapsed >= stableFor;
            });
        }

        public void SaveErrorList(string name)
        {
            lastErrors = VisualStudioAutomation.ReadErrorList(dte);
            WriteJson(name, lastErrors);
        }

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

        private void WaitUntil(string description, Func<bool> condition)
        {
            var watch = Stopwatch.StartNew();
            COMException lastComError = null;
            while (watch.Elapsed < PhaseTimeout)
            {
                if (ideProcess != null && ideProcess.HasExited) { throw new InvalidOperationException("Visual Studio exited during " + description); }
                try { if (condition()) { return; } }
                catch (COMException exception) { lastComError = exception; }
                Thread.Sleep(500);
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
            File.WriteAllText(Path.Combine(resultsDirectory, logName), output.GetAwaiter().GetResult() + error.GetAwaiter().GetResult());
            if (process.ExitCode != 0) { throw new InvalidOperationException($"Process failed ({process.ExitCode}): {path}"); }
            return output.GetAwaiter().GetResult();
        }

        private static string Quote(string path)
        {
            if (path.Contains("\"") || path.Contains("\r") || path.Contains("\n")) { throw new ArgumentException("Invalid path."); }
            return "\"" + path + "\"";
        }

        private void WriteJson(string name, object value) => File.WriteAllText(Path.Combine(resultsDirectory, name), JsonConvert.SerializeObject(value, Newtonsoft.Json.Formatting.Indented));

        public void Dispose()
        {
            try
            {
                if (dte != null)
                {
                    try { File.WriteAllText(Path.Combine(resultsDirectory, "SonarOutput.txt"), VisualStudioAutomation.ReadSonarOutput(dte)); }
                    catch (COMException exception) { context.WriteLine("Could not capture Sonar output: " + exception.Message); }
                }
                report["finishedAt"] = DateTime.UtcNow.ToString("o");
                WriteJson("result.json", report);
                WriteJson("last-error-list.json", lastErrors);
                foreach (var file in Directory.GetFiles(resultsDirectory)) { context.AddResultFile(file); }
            }
            finally
            {
                if (ideProcess != null)
                {
                    if (!ideProcess.HasExited) { ideProcess.Kill(); }
                    ideProcess.Dispose();
                }
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
        }
    }
}
