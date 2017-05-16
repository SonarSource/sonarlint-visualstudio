/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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

using EnvDTE;
using FluentAssertions;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Resources;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class VsShellUtilsTests
    {
        [TestMethod]
        public void VsShellUtils_ActivateSolutionExplorer()
        {
            // Arrange
            var serviceProvider = new ConfigurableServiceProvider();
            var dteMock = new DTEMock();
            serviceProvider.RegisterService(typeof(DTE), dteMock);

            // Sanity
            dteMock.ToolWindows.SolutionExplorer.Window.Active.Should().BeFalse();

            // Act
            VsShellUtils.ActivateSolutionExplorer(serviceProvider);

            // Assert
            dteMock.ToolWindows.SolutionExplorer.Window.Active.Should().BeTrue("Expected to become Active");
        }

        [TestMethod]
        public void VsShellUtils_SaveSolution_Silent()
        {
            // Arrange
            var serviceProvider = new ConfigurableServiceProvider();
            var solution = new SolutionMock();
            serviceProvider.RegisterService(typeof(SVsSolution), solution);
            solution.SaveSolutionElementAction = (options, hierarchy, docCookie) =>
            {
                ((__VSSLNSAVEOPTIONS)options).Should().Be(__VSSLNSAVEOPTIONS.SLNSAVEOPT_SaveIfDirty, "Unexpected save options");
                hierarchy.Should().BeNull("Expecting the scope to be the whole solution");
                docCookie.Should().Be(0U, "Expecting the scope to be the whole solution");

                return VSConstants.S_OK;
            };

            // Act + Assert
            VsShellUtils.SaveSolution(serviceProvider, silent: true).Should().BeTrue();
        }

        [TestMethod]
        public void VsShellUtils_SaveSolution_Prompt()
        {
            // Arrange
            var serviceProvider = new ConfigurableServiceProvider();
            var solution = new SolutionMock();
            serviceProvider.RegisterService(typeof(SVsSolution), solution);
            int hrResult = 0;
            solution.SaveSolutionElementAction = (options, hierarchy, docCookie) =>
            {
                ((__VSSLNSAVEOPTIONS)options).Should().Be(__VSSLNSAVEOPTIONS.SLNSAVEOPT_SaveIfDirty | __VSSLNSAVEOPTIONS.SLNSAVEOPT_PromptSave, "Unexpected save options");
                hierarchy.Should().BeNull("Expecting the scope to be the whole solution");
                docCookie.Should().Be(0U, "Expecting the scope to be the whole solution");

                return hrResult;
            };

            // Case 1: user selected 'Yes'
            hrResult = VSConstants.S_OK; //0

            // Act + Assert
            VsShellUtils.SaveSolution(serviceProvider, silent: false).Should().BeTrue();

            // Case 2: user selected 'No'
            hrResult = VSConstants.S_FALSE; //1

            // Act + Assert
            VsShellUtils.SaveSolution(serviceProvider, silent: false).Should().BeFalse();

            // Case 3: user selected 'Cancel'
            hrResult = VSConstants.E_ABORT;

            // Act + Assert
            VsShellUtils.SaveSolution(serviceProvider, silent: false).Should().BeFalse();
        }

        [TestMethod]
        public void VsShellUtils_GetOrCreateSonarLintOutputPane()
        {
            // Arrange
            var outputWindow = new ConfigurableVsOutputWindow();

            var serviceProvider = new ConfigurableServiceProvider();
            serviceProvider.RegisterService(typeof(SVsOutputWindow), outputWindow);

            // Act
            IVsOutputWindowPane pane = VsShellUtils.GetOrCreateSonarLintOutputPane(serviceProvider);

            // Assert
            outputWindow.AssertPaneExists(VsShellUtils.SonarLintOutputPaneGuid);
            pane.Should().NotBeNull();

            var sonarLintPane = pane as ConfigurableVsOutputWindowPane;
            if (sonarLintPane == null)
            {
                FluentAssertions.Execution.Execute.Assertion.FailWith($"Expected returned pane to be of type {nameof(ConfigurableVsOutputWindowPane)}");
            }

            sonarLintPane.IsActivated.Should().BeTrue("Expected pane to be activated");
            sonarLintPane.Name.Should().Be(Strings.SonarLintOutputPaneTitle, "Unexpected pane name.");
        }
    }
}