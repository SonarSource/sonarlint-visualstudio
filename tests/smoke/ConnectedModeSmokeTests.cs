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
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SonarLint.VisualStudio.Smoke
{
    [TestClass]
    [DoNotParallelize]
    public class ConnectedModeSmokeTests
    {
        private const string NestedBranches = "        if (value > 0) { if (value > 1) { return 1; } }";
        public TestContext TestContext { get; set; }

        [STATestMethod]
        [TestCategory("ConnectedInstalledVsixSmoke")]
        public void InstalledExtension_UsesBoundServerProfile_AndClearsIssueAfterEdit()
        {
            var results = Environment.GetEnvironmentVariable("SMOKE_RESULTS_DIRECTORY")
                ?? throw new InvalidOperationException("SMOKE_RESULTS_DIRECTORY is required.");
            var vsix = Environment.GetEnvironmentVariable("SMOKE_VSIX_PATH");
            // Establish the unbound control with exactly the same package and sample.
            using (var standalone = new VisualStudioSession(vsix, Path.Combine(results, "standalone"), TestContext))
            {
                try
                {
                    standalone.PrepareSample("Unbound" + Guid.NewGuid().ToString("N"));
                    ArrangeComplexity(standalone.SourcePath);
                    standalone.InstallAndLaunch();
                    standalone.OpenSample();
                    standalone.WaitForFinding();
                    standalone.WaitForSampleFindings("standalone-control", rows => !rows.Any(IsServerFinding));
                    standalone.MarkPassed();
                }
                catch (Exception e) { standalone.RecordFailure(e); throw; }
            }

            var solutionName = "Connected" + Guid.NewGuid().ToString("N");
            using var binding = new ConnectedServerFixture(solutionName + ".sln", results);
            using var connected = new VisualStudioSession(vsix, Path.Combine(results, "connected"), TestContext);
            connected.PrepareSample(solutionName);
            ArrangeComplexity(connected.SourcePath);
            try
            {
                connected.InstallAndLaunch();
                connected.OpenSample();
                connected.WaitForSampleFindings("server-profile-applied", rows =>
                    rows.Count(IsServerFinding) == 1 && !rows.Any(IsStandaloneFinding));

                var edited = File.ReadAllText(connected.SourcePath).Replace(NestedBranches, "");
                connected.ReplaceSampleSource(edited);
                connected.WaitForSampleFindings("after-connected-edit", rows =>
                    !rows.Any(IsServerFinding) && !rows.Any(IsStandaloneFinding), TimeSpan.FromSeconds(5));
                binding.SaveEvidence();
                connected.MarkPassed();
            }
            catch (Exception e) { connected.RecordFailure(e); throw; }
        }

        private static void ArrangeComplexity(string sourcePath)
        {
            var source = File.ReadAllText(sourcePath).Replace("Run()", "Run(int value)")
                .Replace("        return 0;", NestedBranches + Environment.NewLine + "        return 0;");
            File.WriteAllText(sourcePath, source);
        }

        private static bool IsServerFinding(ErrorListEntry row) => row.Line == 3
            && row.Description == ConnectedServerFixture.ExpectedFinding;

        private static bool IsStandaloneFinding(ErrorListEntry row) => row.Line == 5
            && row.Description == "Remove the unused local variable 'smokeUnused'.";
    }
}
