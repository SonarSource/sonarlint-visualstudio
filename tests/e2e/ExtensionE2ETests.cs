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
            using var messageFilter = ComMessageFilter.Register();
            using var ide = new VisualStudioSession(
                Environment.GetEnvironmentVariable("E2E_VSIX_PATH"),
                Environment.GetEnvironmentVariable("E2E_RESULTS_DIRECTORY"),
                TestContext,
                messageFilter);
            try
            {
                ide.InstallAndLaunch();
                ide.OpenSample();

                var finding = ide.WaitForFinding();
                Assert.AreEqual(5, finding.Line);
                Assert.AreEqual("Remove the unused local variable 'e2eUnused'.", finding.Description);
                ide.SaveErrorList("errors-before-fix.json");

                ide.RemoveUnusedVariableAndSave();

                ide.WaitForFindingToDisappear(TimeSpan.FromSeconds(5));
                Assert.AreEqual(0, ide.MatchingFindings().Length);
                Assert.AreEqual(1, ide.RemainingFindings().Length, "The unedited Sonar finding must remain visible.");
                ide.SaveErrorList("errors-after-fix.json");
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
