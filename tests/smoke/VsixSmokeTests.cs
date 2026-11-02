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
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using Xunit;
using Xunit.Abstractions;
using Xunit.Harness;
using DteService = VSInterop::EnvDTE._DTE;
using DTE2 = VSInterop::EnvDTE80.DTE2;

namespace SonarLint.VisualStudio.Smoke
{
    public class VsixSmokeTests
    {
        private readonly ITestOutputHelper output;

        public VsixSmokeTests(ITestOutputHelper output) => this.output = output;

        [IdeFact(MinVersion = VisualStudioVersion.VS18, MaxVersion = VisualStudioVersion.VS18,
            RootSuffix = SmokeConfiguration.RootSuffix, MaxAttempts = 1)]
        public async Task InstalledExtension_DetectsIssue_AndClearsItAfterFix()
        {
            // Fail if the custom framework did not actually execute inside the IDE.
            using var process = Process.GetCurrentProcess();
            Assert.Equal("devenv", process.ProcessName);
            Assert.NotEqual(0, process.SessionId);
            Assert.True(Environment.UserInteractive);
            Assert.NotNull(Application.Current);
            Assert.True(Application.Current.Dispatcher.CheckAccess());
            Assert.Equal(SmokeConfiguration.RootSuffix, Environment.GetEnvironmentVariable("VSROOTSUFFIX"));
            var dte = (DTE2)GlobalServiceProvider.ServiceProvider.GetService(typeof(DteService));
            Assert.NotNull(dte);

            using var scenario = new SmokeScenario(dte, output);
            try
            {
                scenario.PrepareSample();
                await scenario.OpenSampleAsync();

                var finding = await scenario.WaitForFindingAsync();
                Assert.Equal(5, finding.Line);
                Assert.Equal(SmokeScenario.FindingMessage, finding.Description);
                scenario.SaveErrorList("errors-before-fix.json");

                scenario.RemoveUnusedVariableAndSave();

                await scenario.WaitForFindingToDisappearAsync(TimeSpan.FromSeconds(5));
                Assert.Empty(scenario.MatchingFindings());
                scenario.SaveErrorList("errors-after-fix.json");
                scenario.MarkPassed();
            }
            catch (Exception exception)
            {
                scenario.RecordFailure(exception);
                throw;
            }
        }
    }
}
