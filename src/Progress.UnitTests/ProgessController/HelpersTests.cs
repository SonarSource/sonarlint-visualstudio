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

using SonarLint.VisualStudio.Progress.Controller;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace SonarLint.VisualStudio.Progress.UnitTests
{
    public class HelpersTests
    {
        [TestMethod]
        public void Helpers_RunOnFinished()
        {
            // Setup
            ConfigurableProgressEvents progressEvents = new ConfigurableProgressEvents();
            ProgressControllerResult? result = null;
            Action<ProgressControllerResult> action = (r) => result = r;

            foreach (ProgressControllerResult progressResult in Enum.GetValues(typeof(ProgressControllerResult)))
            {
                result = null;
                Helpers.RunOnFinished(progressEvents, action);

                // Act
                progressEvents.InvokeFinished(progressResult);

                // Verify
                Assert.AreEqual(progressResult, result, "Action was not called");
                progressEvents.AssertAllEventsAreUnregistered();
            }
        }
    }
}
