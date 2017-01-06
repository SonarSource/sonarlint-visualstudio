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

using SonarLint.VisualStudio.Progress.Controller.ErrorNotification;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace SonarLint.VisualStudio.Progress.UnitTests
{
    /// <summary>
    /// Tests for <see cref="ErrorNotificationMananger"/>
    /// </summary>
    [TestClass]
    public class ErrorNotificationManagerTests
    {
        private ConfigurableServiceProvider serviceProvider;
        private ErrorNotificationManager testSubject;

        #region Test plumbing
        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestInitialize()
        {
            this.serviceProvider = new ConfigurableServiceProvider();
            this.testSubject = new ErrorNotificationManager();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            this.serviceProvider = null;
            this.testSubject = null;
        }
        #endregion

        #region Tests
        [TestMethod]
        [Description("Tests adding and removing notifiers")]
        public void ErrorNotificationMananger_EndToEnd()
        {
            ConfigurableErrorNotifier testNotifier = new ConfigurableErrorNotifier();

            // Should not throw
            this.Notify();

            // Add notifier
            this.testSubject.AddNotifier(testNotifier);
            this.Notify();
            testNotifier.AssertExcepections(1);

            // Cleanup
            this.testSubject.RemoveNotifier(testNotifier);

            // Add same notifier multiple times (no op)
            this.testSubject.AddNotifier(testNotifier);
            testNotifier.Reset();
            this.Notify();
            testNotifier.AssertExcepections(1);

            // Remove single instance
            this.testSubject.RemoveNotifier(testNotifier);
            testNotifier.Reset();
            this.Notify();
            testNotifier.AssertExcepections(0);

            // Remove non existing instance
            this.testSubject.RemoveNotifier(testNotifier);
            testNotifier.Reset();
            this.Notify();
            testNotifier.AssertExcepections(0);
        }
        #endregion

        #region Helpers
        private void Notify()
        {
            ((IProgressErrorNotifier)this.testSubject).Notify(new Exception(this.TestContext.TestName));
        }
        #endregion
    }
}
