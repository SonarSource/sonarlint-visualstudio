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
using Microsoft.VisualStudio.Shell.Interop;
using NuGet;
using NuGet.VisualStudio;
using SonarLint.VisualStudio.Integration.Binding;
using System;
using Xunit;

namespace SonarLint.VisualStudio.Integration.UnitTests.Binding
{

    public class NuGetHelperTests
    {
        #region Tests
        [Fact]
        public void TryInstallPackage_WithNullServiceProvider_ThrowsArgumentNullException()
        {
            // Arrange + Act
            Action act = () => NuGetHelper.TryInstallPackage(null, new ProjectMock("123"), "123");

            // Assert
            act.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("serviceProvider");
        }

        [Fact]
        public void TryInstallPackage_WithNullProject_ThrowsArgumentNullException()
        {
            // Arrange + Act
            Action act = () => NuGetHelper.TryInstallPackage(CreateServiceProvider(), null, "123");

            // Assert
            act.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("project");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void TryInstallPackage_WithNullOrEmptyOrWhiteSpacePackageId_ThrowsArgumentNullException(string value)
        {
            // Arrange + Act
            Action act = () => NuGetHelper.TryInstallPackage(CreateServiceProvider(), new ProjectMock("123"), value);

            // Assert
            act.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("packageId");
        }

        [Fact]
        public void NuGetHelper_HandleFailures()
        {
            // Arrange
            ConfigurableServiceProvider sp = CreateServiceProvider();
            var outputWindow = new ConfigurableVsOutputWindow();
            var outputPane = outputWindow.GetOrCreateSonarLintPane();
            sp.RegisterService(typeof(SVsOutputWindow), outputWindow);

            // Case 1: No MEF service
            // Act + Assert
            using (new AssertIgnoreScope()) // Missing MEF service
            {
                NuGetHelper.TryInstallPackage(sp, new ProjectMock("prj"), "pcg")
                    .Should().BeFalse("No MEF service should be resulted with a false returned value");
            }
            outputPane.AssertOutputStrings(0);

            // Case 2: Exception from the service
            sp.RegisterService(typeof(SComponentModel), ConfigurableComponentModel.CreateWithExports(MefTestHelpers.CreateExport<IVsPackageInstaller>(new ConfigurablePackageInstaller(simulateInstallerException: true))), replaceExisting: true);
            // Act + Assert
            NuGetHelper.TryInstallPackage(sp, new ProjectMock("prj"), "pcg")
                .Should().BeFalse("Non critical exception should result with a false returned value");
            outputPane.AssertOutputStrings(1);
        }

        [Fact]
        public void NuGetHelper_SuccessfulCalls()
        {
            // Arrange
            var package = new PackageName(Guid.NewGuid().ToString("N"), new SemanticVersion("1.0"));
            var availablePackages = new[] { package };

            ConfigurableServiceProvider sp = CreateServiceProvider();
            sp.RegisterService(typeof(SComponentModel), ConfigurableComponentModel.CreateWithExports(MefTestHelpers.CreateExport<IVsPackageInstaller>(new ConfigurablePackageInstaller(availablePackages, simulateInstallerException: false))), replaceExisting: true);

            // Act + Assert
            NuGetHelper.TryInstallPackage(sp, new ProjectMock("prj"), package.Id, package.Version.ToNormalizedString())
                .Should().BeTrue("The package is expected to be installed successfully");
        }
        #endregion

        #region Helpers
        private static ConfigurableServiceProvider CreateServiceProvider()
        {
            ConfigurableServiceProvider sp = new ConfigurableServiceProvider();
            sp.RegisterService(typeof(SComponentModel), new ConfigurableComponentModel());
            return sp;
        }
        #endregion
    }
}
