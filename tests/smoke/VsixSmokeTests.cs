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

namespace SonarLint.VisualStudio.Smoke
{
    [TestClass]
    [DoNotParallelize]
    public class VsixSmokeTests
    {
        public TestContext TestContext { get; set; }

        [STATestMethod]
        [TestCategory("InstalledVsixSmoke")]
        public void InstalledExtension_DetectsIssue_AndClearsItAfterFix()
        {
            using var ide = new VisualStudioSession(
                Environment.GetEnvironmentVariable("SMOKE_VSIX_PATH"),
                Environment.GetEnvironmentVariable("SMOKE_RESULTS_DIRECTORY"),
                TestContext);
            try
            {
                ide.InstallAndLaunch();
                ide.OpenSample();

                var finding = ide.WaitForFinding();
                Assert.AreEqual(5, finding.Line);
                Assert.AreEqual("Remove the unused local variable 'smokeUnused'.", finding.Description);
                ide.SaveErrorList("errors-before-fix.json");

                ide.RemoveUnusedVariableAndSave();

                ide.WaitForFindingToDisappear(TimeSpan.FromSeconds(5));
                Assert.AreEqual(0, ide.MatchingFindings().Length);
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
