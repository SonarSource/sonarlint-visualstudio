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
    public class ExtensionE2ETests
    {
        public TestContext TestContext { get; set; }

        [STATestMethod]
        [TestCategory("ExtensionE2E")]
        public void InstalledExtension_DetectsIssue_AndClearsItAfterFix()
        {
            using var ide = new VisualStudioSession(E2ESettings.FromEnvironment(), TestContext);
            try
            {
                ide.InstallAndLaunch();
                var fixture = ide.CopyFixture();
                ide.OpenSolution(Path.Combine(fixture, "Sample.sln"), Path.Combine(fixture, "Sample.cs"));
                var unused = SampleFindings.UnusedVariable(ide.DocumentPath, "e2eUnused");
                var remaining = SampleFindings.UnusedVariable(ide.DocumentPath, "e2eRemaining");

                var before = ide.WaitForErrorList("detect-findings", errors => errors.Any(unused) && errors.Any(remaining));
                ide.SaveErrorList("errors-before-fix.json", before);
                Assert.AreEqual(5, before.Single(unused).Line, "Sonar must report e2eUnused on its declaration line.");
                Assert.AreEqual(6, before.Single(remaining).Line, "Sonar must report e2eRemaining on its declaration line.");

                ide.DeleteLineAndSave(5);
                Assert.IsFalse(File.ReadAllText(ide.DocumentPath).Contains("e2eUnused"), "The editor must save the fixed sample to disk.");

                var after = ide.WaitForErrorListStable("clear-finding",
                    errors => !errors.Any(unused) && errors.Count(remaining) == 1,
                    TimeSpan.FromSeconds(5));
                ide.SaveErrorList("errors-after-fix.json", after);
                Assert.AreEqual(5, after.Single(remaining).Line, "The unedited finding must follow its declaration up one line.");
                ide.MarkPassed();
            }
            catch (Exception exception)
            {
                ide.RecordFailure(exception);
                throw;
            }
        }
    }
}
