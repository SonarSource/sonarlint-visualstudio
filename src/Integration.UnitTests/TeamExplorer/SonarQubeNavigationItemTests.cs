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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Resources;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using System;

namespace SonarLint.VisualStudio.Integration.UnitTests.TeamExplorer
{
    [TestClass]
    public class SonarQubeNavigationItemTests
    {
        [TestMethod]
        public void SonarQubeNavigationItem_Execute()
        {
            // Setup
            var serviceProvider = new ConfigurableServiceProvider();
            var controller = new ConfigurableTeamExplorerController();

            var testSubject = new SonarQubeNavigationItem(controller);

            // Act
            testSubject.Execute();

            // Verify
            controller.AssertExpectedNumCallsShowConnectionsPage(1);
        }

        [TestMethod]
        public void SonarQubeNavigationItem_Ctor()
        {
            // Setup
            var serviceProvider = new ConfigurableServiceProvider();
            var controller = new ConfigurableTeamExplorerController();

            // Act
            var testSubject = new SonarQubeNavigationItem(controller);

            // Verify
            Assert.IsTrue(testSubject.IsVisible, "Nav item should be visible");
            Assert.IsTrue(testSubject.IsEnabled, "Nav item should be enabled");
            Assert.AreEqual(Strings.TeamExplorerPageTitle, testSubject.Text, "Unexpected nav text");

            Assert.IsNotNull(testSubject.Icon, "Icon should not be null");
        }

        [TestMethod]
        public void SonarQubeNavigationItem_Ctor_NullArgChecks()
        {
            Exceptions.Expect<ArgumentNullException>(() => new SonarQubeNavigationItem(null));
        }
    }
}
