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

using System;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Progress;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class ProgressNotificationListenerTests
    {
        [TestMethod]
        public void ProgressNotificationListener_ArgChecks()
        {
            Exceptions.Expect<ArgumentNullException>(() => new ProgressNotificationListener(null, new ConfigurableProgressEvents()));
            Exceptions.Expect<ArgumentNullException>(() => new ProgressNotificationListener(new ConfigurableServiceProvider(), null));
        }

        [TestMethod]
        public void ProgressNotificationListener_RespondToStepExecutionChangedEvent()
        {
            // Setup
            var serviceProvider = new ConfigurableServiceProvider();

            var outputWindow = new ConfigurableVsOutputWindow();
            var outputWindowPane = outputWindow.GetOrCreateSonarLintPane();
            serviceProvider.RegisterService(typeof(SVsOutputWindow), outputWindow);

            var progressEvents = new ConfigurableProgressEvents();
            var testSubject = new ProgressNotificationListener(serviceProvider, progressEvents);
            string message1 = "Hello world";
            string formattedMessage2 = "Bye bye";

            // Step 1: no formatting
            // Act
            progressEvents.SimulateStepExecutionChanged(message1, 0);

            // Verify
            outputWindowPane.AssertOutputStrings(message1);

            // Step 2: same message as before (ignore)
            // Act
            progressEvents.SimulateStepExecutionChanged(message1, 0);

            // Verify
            outputWindowPane.AssertOutputStrings(message1);

            // Step 3: whitespace message
            // Act
            progressEvents.SimulateStepExecutionChanged(" \t", 0);

            // Verify
            outputWindowPane.AssertOutputStrings(message1);

            // Step 4: formatting
            testSubject.MessageFormat = "XXX{0}YYY";
            // Act
            progressEvents.SimulateStepExecutionChanged(formattedMessage2, 0);

            // Verify
            outputWindowPane.AssertOutputStrings(message1, "XXX" + formattedMessage2 + "YYY");

            // Step 5: different message than the previous one
            testSubject.MessageFormat = null;
            // Act
            progressEvents.SimulateStepExecutionChanged(message1, 0);

            // Verify
            outputWindowPane.AssertOutputStrings(message1, "XXX" + formattedMessage2 + "YYY", message1);

            // Step 6: dispose
            testSubject.Dispose();
            // Act
            progressEvents.SimulateStepExecutionChanged("123", 0);

            // Verify
            outputWindowPane.AssertOutputStrings(message1, "XXX" + formattedMessage2 + "YYY", message1);
        }
    }
}
