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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Progress.Controller;
using SonarLint.VisualStudio.Progress.Threading;

namespace SonarLint.VisualStudio.Progress.UnitTests
{
    /// <summary>
    /// Test extension methods
    /// </summary>
    public static class SequentialProgressControllerHelper
    {
        /// <summary>
        /// Initializes the specified controller with custom error handler that will allow assertions to be raised
        /// </summary>
        /// <param name="controller">Controller instance to initialize</param>
        /// <param name="definitions">The step definitions to initialize the controller with</param>
        /// <returns>The notifier that was used for test assert exceptions</returns>
        public static ConfigurableErrorNotifier InitializeWithTestErrorHandling(SequentialProgressController controller, params ProgressStepDefinition[] definitions)
        {
            controller.Initialize(definitions);
            return ConfigureToThrowAssertExceptions(controller);
        }

        /// <summary>
        /// The <see cref="ProgressControllerStep"/> which are used by default will swallow the assert exceptions
        /// which means that investigating why something is failing requires more time and effort.
        /// This extension method will record the first <see cref="UnitTestAssertException"/> which was thrown during
        /// execution and will rethrow it on a way that will allow the test to fail and see the original stack
        /// that caused the test failure (on Finished event)
        /// </summary>
        /// <param name="controller">The controller to configure</param>
        /// <returns>The notifier that was used for configuration of the assert exception</returns>
        public static ConfigurableErrorNotifier ConfigureToThrowAssertExceptions(SequentialProgressController controller)
        {
            controller.Should().NotBeNull("Controller argument is required");
            controller.Steps.Should().NotBeNull("Controller needs to be initialized");

            ConfigurableErrorNotifier errorHandler = new ConfigurableErrorNotifier();
            controller.ErrorNotificationManager.AddNotifier(errorHandler);

            UnitTestAssertException originalException = null;

            // Controller.Finished is executed out of the awaitable state machine and on the calling (UI) thread
            // which means that at this point the test runtime engine will be able to catch it and fail the test
            EventHandler<ProgressControllerFinishedEventArgs> onFinished = null;
            onFinished = (s, e) =>
            {
                // Need to register on the UI thread
                VsThreadingHelper.RunTask(controller, Microsoft.VisualStudio.Shell.VsTaskRunContext.UIThreadNormalPriority, () =>
                {
                    controller.Finished -= onFinished;
                }).Wait();

                // Satisfy the sequential controller verification code
                e.Handled();

                if (originalException != null)
                {
                    e.Result.Should().Be(ProgressControllerResult.Failed, "Expected to be failed since the assert failed which causes an exception");
                    throw new RestoredUnitTestAssertException(originalException.Message, originalException);
                }
            };

            // Need to register on the UI thread
            VsThreadingHelper.RunTask(controller, Microsoft.VisualStudio.Shell.VsTaskRunContext.UIThreadNormalPriority, () =>
            {
                controller.Finished += onFinished;
            }).Wait();

            errorHandler.NotifyAction = (e) =>
            {
                // Only the first one
                if (originalException == null)
                {
                    originalException = e as UnitTestAssertException;
                }
            };
            return errorHandler;
        }

        #region Test helper class RestoredUnitTestAssertException : UnitTestAssertException

        private class RestoredUnitTestAssertException : UnitTestAssertException
        {
            public RestoredUnitTestAssertException()
            {
            }

            public RestoredUnitTestAssertException(string message)
                : base(message)
            {
            }

            public RestoredUnitTestAssertException(string message, Exception innerException)
                : base(message, innerException)
            {
            }

            protected RestoredUnitTestAssertException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
                : base(info, context)
            {
            }
        }

        #endregion Test helper class RestoredUnitTestAssertException : UnitTestAssertException
    }
}