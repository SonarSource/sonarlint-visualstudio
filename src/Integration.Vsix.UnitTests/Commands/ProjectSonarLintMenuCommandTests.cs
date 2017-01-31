/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA and Microsoft Corporation
 * mailto: contact AT sonarsource DOT com
 *
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using FluentAssertions;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
 using Xunit;
using SonarLint.VisualStudio.Integration.Vsix;
using System;
using System.Windows.Threading;

namespace SonarLint.VisualStudio.Integration.UnitTests.Commands
{
    public class ProjectSonarLintMenuCommandTests
    {
        private ConfigurableVsProjectSystemHelper projectSystem;
        private IServiceProvider serviceProvider;

        public ProjectSonarLintMenuCommandTests()
        {
            var provider = new ConfigurableServiceProvider();
            this.projectSystem = new ConfigurableVsProjectSystemHelper(this.serviceProvider);
            provider.RegisterService(typeof(IProjectSystemHelper), this.projectSystem);

            var host = new ConfigurableHost(provider, Dispatcher.CurrentDispatcher);
            var propertyManager = new ProjectPropertyManager(host);
            var mefExports = MefTestHelpers.CreateExport<IProjectPropertyManager>(propertyManager);
            var mefModel = ConfigurableComponentModel.CreateWithExports(mefExports);
            provider.RegisterService(typeof(SComponentModel), mefModel);

            this.serviceProvider = provider;
        }

        #region Tests
        [Fact]
        public void ProjectSonarLintMenuCommand_QueryStatus_NoProjects_IsDisableIsHidden()
        {
            // Arrange
            OleMenuCommand command = CommandHelper.CreateRandomOleMenuCommand();

            var testSubject = new ProjectSonarLintMenuCommand(serviceProvider);

            // Act
            testSubject.QueryStatus(command, null);

            // Assert
            command.Enabled.Should().BeFalse( "Expected command to be disabled");
            command.Visible.Should().BeFalse( "Expected command to be hidden");
        }

        [Fact]
        public void ProjectSonarLintMenuCommand_QueryStatus_HasUnsupportedProject_IsDisabledIsHidden()
        {
            // Arrange
            OleMenuCommand command = CommandHelper.CreateRandomOleMenuCommand();

            var testSubject = new ProjectSonarLintMenuCommand(serviceProvider);

            var p1 = new ProjectMock("cs.proj");
            p1.SetCSProjectKind();
            var p2 = new ProjectMock("cpp.proj");

            this.projectSystem.SelectedProjects = new[] { p1, p2 };

            // Act
            testSubject.QueryStatus(command, null);

            // Assert
            command.Enabled.Should().BeFalse( "Expected command to be disabled");
            command.Visible.Should().BeFalse( "Expected command to be hidden");
        }

        [Fact]
        public void ProjectSonarLintMenuCommand_QueryStatus_AllSupportedProjects_IsEnabledIsVisible()
        {
            // Arrange
            OleMenuCommand command = CommandHelper.CreateRandomOleMenuCommand();

            var testSubject = new ProjectSonarLintMenuCommand(serviceProvider);

            var p1 = new ProjectMock("cs1.proj");
            p1.SetCSProjectKind();
            var p2 = new ProjectMock("cs2.proj");
            p2.SetCSProjectKind();

            this.projectSystem.SelectedProjects = new[] { p1, p2 };

            // Act
            testSubject.QueryStatus(command, null);

            // Assert
            command.Enabled.Should().BeTrue( "Expected command to be enabled");
            command.Visible.Should().BeTrue( "Expected command to be visible");
        }

        #endregion
    }
}
