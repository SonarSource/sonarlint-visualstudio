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
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SonarLint.VisualStudio.E2E
{
    [TestClass]
    [DoNotParallelize]
    public class ConnectedModeE2ETests
    {
        // Fixture/ConnectedMode/Sample.cs.txt: the standalone sample plus a parameter and these
        // nested ifs, which give Run() a cognitive complexity of 3.
        private const string SourceVariant = "ConnectedMode";
        private const string NestedBranches = "        if (value > 0) { if (value > 1) { return 1; } }";
        private const int MethodLine = 3;
        public TestContext TestContext { get; set; }

        [STATestMethod]
        [TestCategory("ConnectedModeE2E")]
        public void InstalledExtension_UsesBoundServerProfile_AndClearsIssueAfterEdit()
        {
            var results = Environment.GetEnvironmentVariable("E2E_RESULTS_DIRECTORY")
                ?? throw new InvalidOperationException("Set E2E_RESULTS_DIRECTORY to a new directory.");
            var vsix = Environment.GetEnvironmentVariable("E2E_VSIX_PATH");
            using var messageFilter = ComMessageFilter.Register();
            // Started first so a server problem fails before any IDE work, and disposed last
            // because the binding fixture deletes its project and profile through the server.
            using var sonarQube = new SonarQubeServer(Path.Combine(results, "server-diagnostics"));

            // Unbound control: the same package and sample raise only the standalone S1481 findings.
            using (var standalone = new VisualStudioSession(vsix, Path.Combine(results, "standalone"), TestContext, messageFilter))
            {
                try
                {
                    standalone.PrepareSample("Unbound" + Guid.NewGuid().ToString("N"), SourceVariant);
                    standalone.InstallAndLaunch();
                    standalone.OpenSample();
                    standalone.WaitForFinding();
                    standalone.WaitForErrors("standalone-control",
                        rows => rows.Count(standalone.IsUnusedVariableFinding) == 2 && !rows.Any(x => IsServerFinding(standalone, x)),
                        TimeSpan.Zero);
                    standalone.MarkPassed();
                }
                catch (Exception exception)
                {
                    standalone.RecordFailure(exception);
                    throw;
                }
            }

            // Bound: the server profile enables only S3776 with threshold 1 and disables S1481.
            // Declared before the IDE session so the IDE exits before the fixture removes its state.
            var solutionName = "Connected" + Guid.NewGuid().ToString("N");
            using var binding = new ConnectedServerFixture(sonarQube, solutionName + ".sln", results);
            using var connected = new VisualStudioSession(vsix, Path.Combine(results, "connected"), TestContext, messageFilter);
            connected.PrepareSample(solutionName, SourceVariant);
            try
            {
                connected.InstallAndLaunch();
                connected.OpenSample();
                connected.WaitForErrors("server-profile-applied",
                    rows => rows.Count(x => IsServerFinding(connected, x)) == 1 && !rows.Any(connected.IsUnusedVariableFinding),
                    TimeSpan.Zero);

                var source = File.ReadAllText(connected.SourcePath);
                Assert.IsTrue(source.Contains(NestedBranches), "The connected-mode fixture must contain the nested ifs.");
                connected.ReplaceSampleSourceAndSave(source.Replace(NestedBranches, ""));
                // The unused variables remain, so S1481 must stay deactivated after the edit.
                connected.WaitForErrors("after-connected-edit",
                    rows => !rows.Any(x => IsServerFinding(connected, x)) && !rows.Any(connected.IsUnusedVariableFinding),
                    TimeSpan.FromSeconds(5));
                binding.SaveEvidence();
                connected.MarkPassed();
            }
            catch (Exception exception)
            {
                connected.RecordFailure(exception);
                throw;
            }
        }

        private static bool IsServerFinding(VisualStudioSession ide, ErrorListEntry entry) =>
            ide.IsSampleFinding(entry, MethodLine, ConnectedServerFixture.ExpectedFinding);
    }
}
