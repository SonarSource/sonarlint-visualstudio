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

using SonarLint.VisualStudio.Integration.Service;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using SonarLint.VisualStudio.Integration.Resources;
using System.Globalization;

namespace SonarLint.VisualStudio.Integration.UnitTests.TeamExplorer
{
    [TestClass]
    public class ProjectViewModelTests
    {
        [TestMethod]
        public void ProjectViewModel_Ctor_NullArgumentChecks()
        {
            Exceptions.Expect<ArgumentNullException>(() =>
            {
                new ProjectViewModel(null, new ProjectInformation());
            });

            Exceptions.Expect<ArgumentNullException>(() =>
            {
                new ProjectViewModel(new ServerViewModel(new ConnectionInformation(new Uri("http://www.com"))), null);
            });
        }

        [TestMethod]
        public void ProjectViewModel_Ctor()
        {
            // Setup
            var projectInfo = new ProjectInformation
            {
                Key = "P1",
                Name = "Project1"
            };
            var serverVM = CreateServerViewModel();

            // Act
            var viewModel = new ProjectViewModel(serverVM, projectInfo);

            // Verify
            Assert.IsFalse(viewModel.IsBound);
            Assert.AreEqual(projectInfo.Key, viewModel.Key);
            Assert.AreEqual(projectInfo.Name, viewModel.ProjectName);
            Assert.AreSame(projectInfo, viewModel.ProjectInformation);
            Assert.AreSame(serverVM, viewModel.Owner);
        }

        [TestMethod]
        public void ProjectViewModel_ToolTipProjectName_RespectsIsBound()
        {
            // Setup
            var projectInfo = new ProjectInformation
            {
                Key = "P1",
                Name = "Project1"
            };
            var viewModel = new ProjectViewModel(CreateServerViewModel(), projectInfo);

            // Test Case 1: When project is bound, should show message with 'bound' marker
            // Act
            viewModel.IsBound = true;

            // Verify
            StringAssert.Contains(viewModel.ToolTipProjectName, viewModel.ProjectName, "ToolTip message should include the project name");
            Assert.AreNotEqual(viewModel.ProjectName, viewModel.ToolTipProjectName, "ToolTip message should also indicate that the project is 'bound'");

            // Test Case 2: When project is NOT bound, should show project name only
            // Act
            viewModel.IsBound = false;

            // Verify
            Assert.AreEqual(viewModel.ProjectName, viewModel.ToolTipProjectName, "ToolTip message should be exactly the same as the project name");
        }

        [TestMethod]
        public void ProjectViewModel_AutomationName()
        {
            // Setup
            var projectInfo = new ProjectInformation
            {
                Key = "P1",
                Name = "Project1"
            };
            var testSubject = new ProjectViewModel(CreateServerViewModel(), projectInfo);

            var expectedNotBound = projectInfo.Name;
            var expectedBound = string.Format(CultureInfo.CurrentCulture, Strings.AutomationProjectBoundDescription, projectInfo.Name);

            // Test case 1: bound
            // Act
            testSubject.IsBound = true;
            var actualBound = testSubject.AutomationName;

            // Verify
            Assert.AreEqual(expectedBound, actualBound, "Unexpected bound SonarQube project description");


            // Test case 2: not bound
            // Act
            testSubject.IsBound = false;
            var actualNotBound = testSubject.AutomationName;

            // Verify
            Assert.AreEqual(expectedNotBound, actualNotBound, "Unexpected unbound SonarQube project description");
        }

        private static ServerViewModel CreateServerViewModel()
        {
            return new ServerViewModel(new ConnectionInformation(new Uri("http://123")));

        }
    }
}
