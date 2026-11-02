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

extern alias VSInterop;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;
using DTE2 = VSInterop::EnvDTE80.DTE2;
using TextSelection = VSInterop::EnvDTE.TextSelection;

namespace SonarLint.VisualStudio.Smoke
{
    // Scenario-specific actions only. Roslyn's framework owns IDE launch, VSIX deployment and teardown.
    internal sealed class SmokeScenario : IDisposable
    {
        internal const string FindingMessage = "Remove the unused local variable 'smokeUnused'.";
        private static readonly TimeSpan PhaseTimeout = TimeSpan.FromSeconds(180);
        private readonly DTE2 dte;
        private readonly ITestOutputHelper output;
        private readonly string resultsDirectory;
        private readonly string assemblyDirectory = Path.GetDirectoryName(typeof(VsixSmokeTests).Assembly.Location);
        private readonly Dictionary<string, object> report = new Dictionary<string, object>
        {
            ["status"] = "failed",
            ["phase"] = "prepare-sample",
            ["startedAt"] = DateTime.UtcNow.ToString("o"),
            ["rootSuffix"] = SmokeConfiguration.RootSuffix,
            ["framework"] = "Microsoft.VisualStudio.Extensibility.Testing.Xunit"
        };
        private ErrorListEntry[] lastErrors = Array.Empty<ErrorListEntry>();
        private string sourcePath;
        private string solutionPath;

        public SmokeScenario(DTE2 dte, ITestOutputHelper output)
        {
            this.dte = dte;
            this.output = output;
            resultsDirectory = Path.GetFullPath(Environment.GetEnvironmentVariable("SMOKE_RESULTS_DIRECTORY")
                ?? throw new ArgumentException("Set SMOKE_RESULTS_DIRECTORY to a new evidence directory."));
            Assert.False(Directory.Exists(resultsDirectory), "Use a fresh evidence directory for each attempt.");
            Directory.CreateDirectory(resultsDirectory);
        }

        public void PrepareSample()
        {
            using (var process = Process.GetCurrentProcess())
            {
                report["ideProcessId"] = process.Id;
                report["visualStudioVersion"] = FileVersionInfo.GetVersionInfo(process.MainModule.FileName).ProductVersion;
            }
            var vsixPath = Path.Combine(assemblyDirectory, "candidate.vsix");
            using (var stream = File.OpenRead(vsixPath))
            using (var sha = SHA256.Create())
            {
                report["vsixSha256"] = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
            }
            using (var archive = ZipFile.OpenRead(vsixPath))
            using (var stream = archive.GetEntry("extension.vsixmanifest")?.Open()
                ?? throw new InvalidDataException("The candidate has no VSIX manifest."))
            using (var reader = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null }))
            {
                var manifest = new XmlDocument { XmlResolver = null };
                manifest.Load(reader);
                var identity = manifest.SelectSingleNode("/*[local-name()='PackageManifest']/*[local-name()='Metadata']/*[local-name()='Identity']") as XmlElement
                    ?? throw new InvalidDataException("The candidate has no VSIX identity.");
                Assert.Equal("SonarLint.0DE19254-1DCA-4479-8EAF-58E5D677FF4D", identity.GetAttribute("Id"));
                var version = identity.GetAttribute("Version");
                Assert.False(string.IsNullOrWhiteSpace(version), "The candidate must identify its version.");
                report["vsixVersion"] = version;
            }
            var fixture = Path.Combine(resultsDirectory, "fixture");
            Directory.CreateDirectory(fixture);
            foreach (var file in Directory.GetFiles(Path.Combine(assemblyDirectory, "Fixture")))
            {
                File.Copy(file, Path.Combine(fixture, Path.GetFileName(file).Replace(".txt", "")));
            }
            sourcePath = Path.Combine(fixture, "Smoke.cs");
            solutionPath = Path.Combine(fixture, "Smoke.sln");
            WriteJson("result.json", report);
        }

        public async Task OpenSampleAsync()
        {
            Phase("open-sample");
            await WaitUntilAsync("the solution to open", () =>
            {
                if (!dte.Solution.IsOpen || !string.Equals(dte.Solution.FullName, solutionPath, StringComparison.OrdinalIgnoreCase))
                {
                    dte.Solution.Open(solutionPath);
                }
                return dte.Solution.IsOpen && string.Equals(dte.Solution.FullName, solutionPath, StringComparison.OrdinalIgnoreCase);
            });
            await WaitUntilAsync("the sample file to open", () =>
            {
                dte.ItemOperations.OpenFile(sourcePath);
                return dte.ActiveDocument?.FullName == sourcePath;
            });
            dte.ExecuteCommand("View.ErrorList");
        }

        public async Task<ErrorListEntry> WaitForFindingAsync()
        {
            Phase("detect-finding");
            await WaitUntilAsync("Sonar's unused-variable finding", () => MatchingFindings().Length > 0);
            var findings = MatchingFindings();
            report["finding"] = findings;
            return findings[0];
        }

        public ErrorListEntry[] MatchingFindings()
        {
            lastErrors = ReadErrorList();
            return lastErrors.Where(x => string.Equals(x.File, sourcePath, StringComparison.OrdinalIgnoreCase)
                && x.Line == 5 && x.Description == FindingMessage).ToArray();
        }

        public void RemoveUnusedVariableAndSave()
        {
            Phase("fix-file");
            var selection = (TextSelection)dte.ActiveDocument.Selection;
            selection.SelectAll();
            selection.Insert(File.ReadAllText(sourcePath).Replace("        int smokeUnused = 42;", ""));
            dte.ActiveDocument.Save();
            Assert.DoesNotContain("int smokeUnused = 42;", File.ReadAllText(sourcePath));
        }

        public async Task WaitForFindingToDisappearAsync(TimeSpan stableFor)
        {
            Phase("clear-finding");
            var clearSince = new Stopwatch();
            await WaitUntilAsync("the finding to remain absent after saving", () =>
            {
                if (MatchingFindings().Length > 0) { clearSince.Reset(); return false; }
                clearSince.Start();
                return clearSince.Elapsed >= stableFor;
            });
        }

        public void SaveErrorList(string name) => WriteJson(name, ReadErrorList());

        private ErrorListEntry[] ReadErrorList()
        {
            var list = dte.ToolWindows.ErrorList;
            list.ShowErrors = list.ShowWarnings = list.ShowMessages = true;
            var items = list.ErrorItems;
            var entries = new ErrorListEntry[items.Count];
            for (var index = 0; index < entries.Length; index++)
            {
                var item = items.Item(index + 1);
                entries[index] = new ErrorListEntry { File = item.FileName, Line = item.Line, Description = item.Description };
            }
            lastErrors = entries;
            return entries;
        }

        private static async Task WaitUntilAsync(string description, Func<bool> condition)
        {
            var watch = Stopwatch.StartNew();
            COMException lastComError = null;
            while (watch.Elapsed < PhaseTimeout)
            {
                Assert.True(Application.Current.Dispatcher.CheckAccess(), "DTE calls must remain on the IDE UI thread.");
                try { if (condition()) { return; } }
                catch (COMException exception) { lastComError = exception; }
                // Yield the IDE UI thread so background analysis and diagnostic refresh can run.
                await Task.Delay(500);
            }
            throw new TimeoutException("Timed out waiting for " + description, lastComError);
        }

        public void MarkPassed()
        {
            report["status"] = "passed";
            Phase("complete");
        }

        public void RecordFailure(Exception exception)
        {
            report["status"] = "failed";
            report["error"] = exception.ToString();
        }

        private void Phase(string phase)
        {
            report["phase"] = phase;
            output.WriteLine($"{DateTime.UtcNow:o} {phase}");
            WriteJson("result.json", report);
        }

        private void WriteJson(string name, object value) => File.WriteAllText(Path.Combine(resultsDirectory, name), JsonConvert.SerializeObject(value, Newtonsoft.Json.Formatting.Indented));

        public void Dispose()
        {
            report["finishedAt"] = DateTime.UtcNow.ToString("o");
            WriteJson("result.json", report);
            WriteJson("last-error-list.json", lastErrors);
            try
            {
                var panes = dte.ToolWindows.OutputWindow.OutputWindowPanes;
                for (var index = 1; index <= panes.Count; index++)
                {
                    var pane = panes.Item(index);
                    if (pane.Name.IndexOf("Sonar", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        var document = pane.TextDocument;
                        File.AppendAllText(Path.Combine(resultsDirectory, "SonarOutput.txt"), pane.Name + Environment.NewLine + document.StartPoint.CreateEditPoint().GetText(document.EndPoint));
                    }
                }
                if (dte.Solution.IsOpen) { dte.Solution.Close(false); }
            }
            catch (COMException exception) { output.WriteLine("Could not finish IDE evidence collection: " + exception.Message); }
        }

        internal sealed class ErrorListEntry
        {
            public string File { get; set; }
            public int Line { get; set; }
            public string Description { get; set; }
        }
    }
}
