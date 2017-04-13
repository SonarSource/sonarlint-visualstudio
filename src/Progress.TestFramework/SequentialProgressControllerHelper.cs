/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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