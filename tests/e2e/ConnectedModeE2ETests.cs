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
        // nested ifs on line 7, which give Run() (line 3) a cognitive complexity of 3.
        private const string SourceVariant = "ConnectedMode";
        private const string NestedBranches = "        if (value > 0) { if (value > 1) { return 1; } }";
        private const int NestedBranchesLine = 7;
        private const int MethodLine = 3;

        // The extension's hidden E2E commands (ConnectedModeMenu.vsct), enabled by VisualStudioSession.
        private const string ConnectedModeCommandSet = "{E5D50D82-248A-4300-A2B2-B640FCC4D4FD}";
        private const int AddConnectionCommandId = 0x103;
        private const int BindCommandId = 0x104;

        public TestContext TestContext { get; set; }

        [STATestMethod]
        [TestCategory("ConnectedModeE2E")]
        public void InstalledExtension_BindsToServer_UsesItsQualityProfile_AndClearsIssueAfterEdit()
        {
            var settings = E2ESettings.FromEnvironment();
            // Started first so a server problem fails before any IDE work, and disposed last.
            using var sonarQube = new SonarQubeServer(Path.Combine(settings.ResultsDirectory, "server-diagnostics"));
            using var ide = new VisualStudioSession(settings, TestContext);
            using var server = new ConnectedServerFixture(sonarQube, settings.ResultsDirectory);
            try
            {
                ide.InstallAndLaunch();
                // The extension keys bindings by solution name, so a unique one cannot pick up an old binding.
                var fixture = ide.CopyFixture(SourceVariant);
                var solution = Path.Combine(fixture, "Connected" + Guid.NewGuid().ToString("N") + ".sln");
                File.Move(Path.Combine(fixture, "Sample.sln"), solution);
                ide.OpenSolution(solution, Path.Combine(fixture, "Sample.cs"));
                var unused = AnyUnusedVariable(ide.DocumentPath);
                var serverFinding = SampleFindings.WithMessage(ide.DocumentPath, ConnectedServerFixture.ExpectedFinding);

                // Before binding: the default rules raise both S1481 findings and no S3776.
                var standalone = ide.WaitForErrorList("standalone-analysis",
                    rows => rows.Count(unused) == 2 && !rows.Any(serverFinding));
                ide.SaveErrorList("standalone-analysis.json", standalone);

                // Bind like a user does: create the connection with a token, then bind the open solution.
                var serverUrl = sonarQube.Url.AbsoluteUri;
                ide.RunCommand("add-connection", ConnectedModeCommandSet, AddConnectionCommandId, $"{serverUrl} {sonarQube.Token}");
                ide.RunCommand("bind", ConnectedModeCommandSet, BindCommandId, $"{serverUrl} {server.ProjectKey}");

                // Bound: the server profile enables only S3776 with threshold 1 and disables S1481.
                var bound = ide.WaitForErrorList("server-profile-applied",
                    rows => rows.Count(serverFinding) == 1 && !rows.Any(unused));
                ide.SaveErrorList("server-profile-applied.json", bound);
                Assert.AreEqual(MethodLine, bound.Single(serverFinding).Line, "S3776 must be reported on the method declaration.");

                Assert.AreEqual(NestedBranches, File.ReadAllLines(ide.DocumentPath)[NestedBranchesLine - 1],
                    "The connected-mode fixture must contain the nested ifs.");
                ide.DeleteLineAndSave(NestedBranchesLine);
                Assert.IsFalse(File.ReadAllText(ide.DocumentPath).Contains(NestedBranches), "The editor must save the edit to disk.");

                // The unused variables remain, so S1481 must stay deactivated after the edit.
                var after = ide.WaitForErrorListStable("after-connected-edit",
                    rows => !rows.Any(serverFinding) && !rows.Any(unused),
                    TimeSpan.FromSeconds(5));
                ide.SaveErrorList("after-connected-edit.json", after);
                server.SaveEvidence();
                ide.MarkPassed();
            }
            catch (Exception exception)
            {
                ide.RecordFailure(exception);
                throw;
            }
        }

        private static Func<ErrorListEntry, bool> AnyUnusedVariable(string documentPath)
        {
            var unused = SampleFindings.UnusedVariable(documentPath, "e2eUnused");
            var remaining = SampleFindings.UnusedVariable(documentPath, "e2eRemaining");
            return entry => unused(entry) || remaining(entry);
        }
    }
}
