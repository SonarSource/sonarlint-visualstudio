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

using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Integration.Binding;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NuGet.VisualStudio;
using System;
using NuGet;

namespace SonarLint.VisualStudio.Integration.UnitTests.Binding
{
    [TestClass]
    public class NuGetHelperTests
    {
        #region Tests
        [TestMethod]
        public void NuGetHelper_ArgChecks()
        {
            Exceptions.Expect<ArgumentNullException>(() => NuGetHelper.TryInstallPackage(null, new ProjectMock("123"), "123"));
            Exceptions.Expect<ArgumentNullException>(() => NuGetHelper.TryInstallPackage(CreateServiceProvider(), null, "123"));
            Exceptions.Expect<ArgumentNullException>(() => NuGetHelper.TryInstallPackage(CreateServiceProvider(), new ProjectMock("123"), null));
        }

        [TestMethod]
        public void NuGetHelper_HandleFailures()
        {
            // Setup
            ConfigurableServiceProvider sp = CreateServiceProvider();
            var outputWindow = new ConfigurableVsOutputWindow();
            var outputPane = outputWindow.GetOrCreateSonarLintPane();
            sp.RegisterService(typeof(SVsOutputWindow), outputWindow);

            // Case 1: No MEF service
            // Act + Verify
            using (new AssertIgnoreScope()) // Missing MEF service
            {
                Assert.IsFalse(NuGetHelper.TryInstallPackage(sp, new ProjectMock("prj"), "pcg"), "No MEF service should be resulted with a false returned value");
            }
            outputPane.AssertOutputStrings(0);

            // Case 2: Exception from the service
            sp.RegisterService(typeof(SComponentModel), ConfigurableComponentModel.CreateWithExports(MefTestHelpers.CreateExport<IVsPackageInstaller>(new ConfigurablePackageInstaller(simulateInstallerException: true))), replaceExisting: true);
            // Act + Verify
            Assert.IsFalse(NuGetHelper.TryInstallPackage(sp, new ProjectMock("prj"), "pcg"), "Non critical exception should result with a false returned value");
            outputPane.AssertOutputStrings(1);
        }

        [TestMethod]
        public void NuGetHelper_SuccessfulCalls()
        {
            // Setup
            var package = new PackageName(Guid.NewGuid().ToString("N"), new SemanticVersion("1.0"));
            var availablePackages = new[] { package };

            ConfigurableServiceProvider sp = CreateServiceProvider();
            sp.RegisterService(typeof(SComponentModel), ConfigurableComponentModel.CreateWithExports(MefTestHelpers.CreateExport<IVsPackageInstaller>(new ConfigurablePackageInstaller(availablePackages, simulateInstallerException: false))), replaceExisting: true);

            // Act + Verify
            Assert.IsTrue(NuGetHelper.TryInstallPackage(sp, new ProjectMock("prj"), package.Id, package.Version.ToNormalizedString()), "The package is expected to be installed successfully");
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
