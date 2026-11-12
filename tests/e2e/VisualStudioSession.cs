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
    internal sealed class VisualStudioSession : IDisposable
    {
        private const string ExtensionId = "SonarLint.0DE19254-1DCA-4479-8EAF-58E5D677FF4D";
        private const string RemainingFindingMessage = "Remove the unused local variable 'e2eRemaining'.";
        private const string FindingMessage = "Remove the unused local variable 'e2eUnused'.";
        private static readonly TimeSpan PhaseTimeout = TimeSpan.FromSeconds(180);
        private readonly string vsixPath;
        private readonly Version expectedVisualStudioVersion;
        private readonly string resultsDirectory;
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
        private string sourcePath;
        private string solutionPath;
        private string profileDirectory;

        public string SourcePath => sourcePath;

        public VisualStudioSession(string vsixPath, string resultsDirectory, TestContext context, ComMessageFilter messageFilter)
        {
            this.vsixPath = Path.GetFullPath(vsixPath ?? throw new ArgumentException("Set E2E_VSIX_PATH to a built VSIX."));
            this.resultsDirectory = Path.GetFullPath(resultsDirectory ?? throw new ArgumentException("Set E2E_RESULTS_DIRECTORY to a new directory."));
            this.context = context;
            this.messageFilter = messageFilter;
            var expectedVersion = Environment.GetEnvironmentVariable("E2E_EXPECTED_VS_VERSION");
            if (!Version.TryParse(expectedVersion, out expectedVisualStudioVersion))
            {
                throw new ArgumentException("Set E2E_EXPECTED_VS_VERSION to the pinned Visual Studio build for this target.");
            }
            if (Directory.Exists(this.resultsDirectory))
            {
                throw new ArgumentException("Use a new results directory for each run.");
            }
            Directory.CreateDirectory(this.resultsDirectory);
            report["rootSuffix"] = rootSuffix;
            using (var currentProcess = Process.GetCurrentProcess())
            {
                report["sessionId"] = currentProcess.SessionId;
            }
            report["userInteractive"] = Environment.UserInteractive;
            report["sessionName"] = Environment.GetEnvironmentVariable("SESSIONNAME");
        }

        public void InstallAndLaunch()
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
            {
                throw new PlatformNotSupportedException("Requires Windows and a full Visual Studio installation.");
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
                if (identity?.Attributes?["Id"]?.Value != ExtensionId)
                {
                    throw new InvalidDataException("The VSIX is not SonarQube for Visual Studio.");
                }
                report["vsixVersion"] = identity.Attributes["Version"].Value;
                File.WriteAllText(Path.Combine(resultsDirectory, "extension.vsixmanifest"), manifest.OuterXml);
            }

            var vswhere = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft Visual Studio", "Installer", "vswhere.exe");
            var inventory = RunProcess(vswhere, "-all -products * -format json", "vswhere.log");
            File.WriteAllText(Path.Combine(resultsDirectory, "visual-studio-inventory.json"), inventory);
            // The major version comes from E2E_EXPECTED_VS_VERSION, so one harness serves every supported VS.
            var expectedMajorPrefix = expectedVisualStudioVersion.Major + ".";
            var instances = JsonConvert.DeserializeObject<Installation[]>(inventory)
                .Where(x => x.InstallationVersion.StartsWith(expectedMajorPrefix, StringComparison.Ordinal)
                    && File.Exists(Path.Combine(x.InstallationPath, "Common7", "IDE", "devenv.exe"))).ToArray();
            if (instances.Length != 1)
            {
                throw new InvalidOperationException($"Expected exactly one full Visual Studio {expectedVisualStudioVersion.Major} IDE; found {instances.Length}.");
            }
            var instance = instances[0];
            report["visualStudioProductId"] = instance.ProductId;
            var expectedProduct = Environment.GetEnvironmentVariable("E2E_EXPECTED_VS_PRODUCT");
            if (!string.IsNullOrEmpty(expectedProduct) && instance.ProductId != expectedProduct)
            {
                throw new InvalidOperationException($"Expected IDE product {expectedProduct}; found {instance.ProductId}.");
            }
            var ideDirectory = Path.Combine(instance.InstallationPath, "Common7", "IDE");
            var targetingPack = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Reference Assemblies", "Microsoft", "Framework", ".NETFramework", "v4.7.2");
            if (!Directory.Exists(targetingPack))
            {
                throw new DirectoryNotFoundException("Missing .NET Framework 4.7.2 targeting pack.");
            }
            report["expectedVisualStudioVersion"] = expectedVisualStudioVersion.ToString();
            report["visualStudioVersion"] = instance.InstallationVersion;
            if (Version.Parse(instance.InstallationVersion) != expectedVisualStudioVersion)
            {
                throw new InvalidOperationException($"Expected VS {expectedVisualStudioVersion}; discovered {instance.InstallationVersion}.");
            }
            report["instanceId"] = instance.InstanceId;
            profileDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Microsoft", "VisualStudio", $"{expectedVisualStudioVersion.Major}.0_{instance.InstanceId}{rootSuffix}");
            report["profileDirectory"] = profileDirectory;
            var devenv = Path.Combine(ideDirectory, "devenv.exe");
            Phase("initialize-profile");
            RunProcess(devenv, $"/RootSuffix {rootSuffix} /ResetSettings {Quote(Path.Combine(ideDirectory, "Profiles", "General.vssettings"))} /Command File.Exit /NoSplash /LCID 1033", "initialize-profile.log");
            Phase("install-vsix");
            RunProcess(Path.Combine(ideDirectory, "VSIXInstaller.exe"),
                $"/quiet {GetVsixInstallationTarget(instance, devenv)} /rootSuffix:{rootSuffix} /logFile:{Quote(Path.Combine(resultsDirectory, "vsix-installer.log"))} {Quote(vsixPath)}", "vsix-process.log");
            Phase("verify-vsix-installation");
            WaitUntil("the signed VSIX in the requested experimental profile", VerifyInstalledExtension);

            // Match Microsoft's integration harness: rebuild extension caches and
            // configuration before launching a newly installed experimental profile.
            Phase("configure-vsix");
            RunProcess(devenv, $"/ClearCache /RootSuffix {rootSuffix}", "clear-cache.log");
            RunProcess(devenv, $"/UpdateConfiguration /RootSuffix {rootSuffix}", "update-configuration.log");
            RunProcess(devenv, $"/RootSuffix {rootSuffix} /ResetSettings {Quote(Path.Combine(ideDirectory, "Profiles", "General.vssettings"))} /Command File.Exit /NoSplash /LCID 1033", "post-install-profile.log");

            Phase("launch-ide");
            ideProcess = Process.Start(new ProcessStartInfo(devenv,
                $"/RootSuffix {rootSuffix} /NoSplash /LCID 1033 /Log {Quote(Path.Combine(resultsDirectory, "ActivityLog.xml"))}") { UseShellExecute = false });
            WaitUntil("the launched IDE's DTE object", () =>
            {
                dte = (EnvDTE80.DTE2)VisualStudioAutomation.FindDte(ideProcess.Id, expectedVisualStudioVersion.Major);
                return dte != null;
            });
        }

        private string GetVsixInstallationTarget(Installation instance, string devenv)
        {
            if (expectedVisualStudioVersion.Major != 17 || expectedVisualStudioVersion.Minor != 0)
            {
                return $"/instanceIds:{instance.InstanceId}";
            }
            // The VS 17.0 RTM installer sends /instanceIds installs to the normal profile even
            // when /rootSuffix is given. Its application/SKU route respects the experimental profile.
            var sku = instance.ProductId switch
            {
                "Microsoft.VisualStudio.Product.Community" => "Community",
                "Microsoft.VisualStudio.Product.Professional" => "Pro",
                "Microsoft.VisualStudio.Product.Enterprise" => "Enterprise",
                _ => throw new InvalidOperationException("Unsupported IDE product: " + instance.ProductId)
            };
            return $"/appidinstallpath:{Quote(devenv)} /appidname:{Quote(instance.DisplayName)} /skuName:{sku} /skuVersion:{instance.InstallationVersion}";
        }

        private bool VerifyInstalledExtension()
        {
            var extensions = Path.Combine(profileDirectory, "Extensions");
            if (!Directory.Exists(extensions)) { return false; }
            foreach (var manifestPath in Directory.GetFiles(extensions, "extension.vsixmanifest", SearchOption.AllDirectories))
            {
                var document = new XmlDocument { XmlResolver = null };
                using var reader = XmlReader.Create(manifestPath,
                    new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
                document.Load(reader);
                var identity = document.SelectSingleNode("/*[local-name()='PackageManifest']/*[local-name()='Metadata']/*[local-name()='Identity']");
                if (identity?.Attributes?["Id"]?.Value == ExtensionId
                    && identity.Attributes["Version"]?.Value == (string)report["vsixVersion"])
                {
                    File.Copy(manifestPath, Path.Combine(resultsDirectory, "installed-extension.vsixmanifest"), true);
                    report["installedExtensionDirectory"] = Path.GetDirectoryName(manifestPath);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Copies the sample into this session's results directory. A source variant
        /// (a subfolder of Fixture) replaces Sample.cs; a custom solution name gives
        /// connected-mode tests a unique binding directory.
        /// </summary>
        public string PrepareSample(string solutionName = "Sample", string sourceVariant = null)
        {
            if (sourcePath != null) { throw new InvalidOperationException("The sample has already been prepared."); }
            var templates = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fixture");
            var fixtureDirectory = Path.Combine(resultsDirectory, "fixture");
            Directory.CreateDirectory(fixtureDirectory);
            foreach (var file in Directory.GetFiles(templates))
            {
                File.Copy(file, Path.Combine(fixtureDirectory, Path.GetFileName(file).Replace(".txt", "")));
            }
            sourcePath = Path.Combine(fixtureDirectory, "Sample.cs");
            if (sourceVariant != null)
            {
                File.Copy(Path.Combine(templates, sourceVariant, "Sample.cs.txt"), sourcePath, true);
            }
            solutionPath = Path.Combine(fixtureDirectory, solutionName + ".sln");
            if (solutionName != "Sample") { File.Move(Path.Combine(fixtureDirectory, "Sample.sln"), solutionPath); }
            return solutionPath;
        }

        public void OpenSample()
        {
            Phase("open-sample");
            if (sourcePath == null) { PrepareSample(); }
            WaitUntil("the solution to open", () =>
            {
                if (!dte.Solution.IsOpen) { dte.Solution.Open(solutionPath); }
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
            WaitUntil("both Sonar S1481 findings in the real Error List", () =>
            {
                var errors = ReadErrors();
                return errors.Any(IsTargetFinding) && errors.Any(IsRemainingFinding);
            });
            var findings = MatchingFindings();
            report["finding"] = findings;
            return findings[0];
        }

        public ErrorListEntry[] MatchingFindings() => ReadErrors().Where(IsTargetFinding).ToArray();

        public ErrorListEntry[] RemainingFindings() => ReadErrors().Where(IsRemainingFinding).ToArray();

        private ErrorListEntry[] ReadErrors() => lastErrors = VisualStudioAutomation.ReadErrorList(dte);

        private bool IsTargetFinding(ErrorListEntry entry) => IsSampleFinding(entry, 5, FindingMessage);

        private bool IsRemainingFinding(ErrorListEntry entry) => IsSampleFinding(entry, 6, RemainingFindingMessage);

        /// <summary>Either of the sample's standalone S1481 findings.</summary>
        public bool IsUnusedVariableFinding(ErrorListEntry entry) => IsTargetFinding(entry) || IsRemainingFinding(entry);

        public bool IsSampleFinding(ErrorListEntry entry, int line, string message) =>
            string.Equals(entry.File, sourcePath, StringComparison.OrdinalIgnoreCase)
            && entry.Line == line && entry.Description == message;

        public void RemoveUnusedVariableAndSave()
        {
            Phase("fix-file");
            var selection = (EnvDTE.TextSelection)dte.ActiveDocument.Selection;
            selection.SelectAll();
            selection.Insert(File.ReadAllText(sourcePath).Replace("        int e2eUnused = 42;", ""));
            dte.ActiveDocument.Save();
            Assert.IsFalse(File.ReadAllText(sourcePath).Contains("int e2eUnused = 42;"), "The editor must save the fixed sample to disk.");
        }

        public void WaitForFindingToDisappear(TimeSpan stableFor)
        {
            Phase("clear-finding");
            WaitForStableErrors("the finding to remain absent after saving",
                errors => !errors.Any(IsTargetFinding) && errors.Any(IsRemainingFinding), stableFor);
        }

        /// <summary>
        /// Waits until the Error List satisfies <paramref name="condition"/> continuously for
        /// <paramref name="stableFor"/>, then saves that snapshot as <paramref name="phase"/>.json.
        /// </summary>
        public ErrorListEntry[] WaitForErrors(string phase, Func<ErrorListEntry[], bool> condition, TimeSpan stableFor)
        {
            Phase(phase);
            WaitForStableErrors(phase, condition, stableFor);
            WriteJson(phase + ".json", lastErrors);
            return lastErrors;
        }

        private void WaitForStableErrors(string description, Func<ErrorListEntry[], bool> condition, TimeSpan stableFor)
        {
            var stableSince = new Stopwatch();
            WaitUntil(description, () =>
            {
                try
                {
                    var busyCallsBeforeRead = messageFilter.BusyCallCount;
                    var errors = ReadErrors();
                    if (messageFilter.BusyCallCount != busyCallsBeforeRead)
                    {
                        // The filter can hide a busy response by retrying the COM call.
                        stableSince.Reset();
                    }
                    if (!condition(errors))
                    {
                        stableSince.Reset();
                        return false;
                    }
                    stableSince.Start();
                    return stableSince.Elapsed >= stableFor;
                }
                catch (COMException)
                {
                    // An unreadable Error List cannot establish a continuous state.
                    stableSince.Reset();
                    throw;
                }
            });
        }

        public void ReplaceSampleSourceAndSave(string text)
        {
            Phase("edit-file");
            var selection = (EnvDTE.TextSelection)dte.ActiveDocument.Selection;
            selection.SelectAll();
            selection.Insert(text);
            dte.ActiveDocument.Save();
            Assert.AreEqual(text, File.ReadAllText(sourcePath), "The IDE must save the exact edit.");
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
                catch (COMException exception) when (exception.HResult == unchecked((int)0x8001010A)
                    || exception.HResult == unchecked((int)0x80010001))
                {
                    lastComError = exception;
                }
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

        private void CaptureProfileDiagnostics()
        {
            var diagnostics = Path.Combine(resultsDirectory, "profile-diagnostics");
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
                    try { File.WriteAllText(Path.Combine(resultsDirectory, "SonarOutput.txt"), VisualStudioAutomation.ReadSonarOutput(dte)); }
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
            [JsonProperty("productId")]
            public string ProductId { get; set; }
            [JsonProperty("displayName")]
            public string DisplayName { get; set; }
        }
    }
}
