/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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

using System.Linq;
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

        [TestMethod]
        public void EnumerateProjectProperties_NullConfigManager_ReturnsNull()
        {
            // Arrange
            var project = new ProjectMock("projectfile.proj")
            {
                ConfigurationManager = null
            };

            // Act
            var result = VsShellUtils.GetProjectProperties(project, "anyproperty");

            // Assert
            result.Should().NotBeNull();
            result.Count().Should().Be(0);
        }

        [TestMethod]
        public void EnumerateProjectProperties_SingleMatchingProperty_NoConfiguration()
        {
            // Arrange
            var project = new ProjectMock("projectfile.proj")
            {
                ConfigurationManager = null
            };
            project.Properties.RegisterKnownProperty("AAA").Value = "aaa value";
            project.Properties.RegisterKnownProperty("BBB").Value = "bbb value";
            project.Properties.RegisterKnownProperty("CCC").Value = "ccc value";

            // Act
            var result = VsShellUtils.GetProjectProperties(project, "BBB");

            // Assert
            result.Should().NotBeNull();
            result.Count().Should().Be(1);
            result.First().Value.Should().Be("bbb value");
        }

        [TestMethod]
        public void EnumerateProjectProperties_WithConfiguration_MultipleMatches()
        {
            // Arrange
            var project = new ProjectMock("projectfile.proj");

            project.Properties.RegisterKnownProperty("no match1").Value = "value1";
            project.Properties.RegisterKnownProperty("no match2").Value = "value2";

            // Expected results
            CreatePropertyForConfiguration(project, "config1", "prop1", "config1 prop1");
            CreatePropertyForConfiguration(project, "config2", "prop1", "config2 prop1");
            CreatePropertyForConfiguration(project, "config4", "prop1", "config4 prop1");

            // Additional non-matching properties
            CreatePropertyForConfiguration(project, "config1", "prop2", "config1 prop2");
            CreatePropertyForConfiguration(project, "config2", "propXXX", "config2 propXXX");
            CreatePropertyForConfiguration(project, "config3", "prop1aa", "config3 prop1aa");

            // Act
            var result = VsShellUtils.GetProjectProperties(project, "prop1");

            // Assert
            result.Should().NotBeNull();
            result.Count().Should().Be(3);
            result.Select(p => p.Value).Should().BeEquivalentTo(
                new string[] { "config1 prop1", "config2 prop1", "config4 prop1" });
        }

        private static PropertyMock CreatePropertyForConfiguration(ProjectMock project,
            string configurationName, string propertyName, object propertyValue)
        {
            ConfigurationMock config = project.ConfigurationManager.Configurations.SingleOrDefault(c => c.ConfigurationName == configurationName);
            if (config == null)
            {
                config = new ConfigurationMock(configurationName);
                project.ConfigurationManager.Configurations.Add(config);
            }

            var prop = config.Properties.RegisterKnownProperty(propertyName);
            prop.Value = propertyValue;
            return prop;
        }
    }
}
