/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Core.UnitTests.Helpers
{
    [TestClass]
    public class WebServiceHelperTests
    {
        [TestMethod]
        public async Task CallSucceeds()
        {
            // Arrange
            var testLogger = new TestLogger();

            // Act
            var result = await WebServiceHelper.SafeServiceCallAsync<bool>(async () => true,
                testLogger).ConfigureAwait(false);

            // Assert
            result.Should().BeTrue();
            testLogger.AssertNoOutputMessages();
        }

        [TestMethod]
        public async Task HttpRequestException_NoInnerException_IsHandledAndLogged()
        {
            // Arrange
            var testLogger = new TestLogger();

            // Act
            await WebServiceHelper.SafeServiceCallAsync<bool>(() => throw new HttpRequestException("outer message"),
                testLogger);

            // Assert
            testLogger.AssertPartialOutputStringExists("outer message");
        }

        [TestMethod]
        public async Task HttpRequestException_InnerWebException_IsHandledAndBothMessagesLogged()
        {
            // Arrange
            var testLogger = new TestLogger();

            // Act
            await WebServiceHelper.SafeServiceCallAsync<bool>(() => 
                throw new HttpRequestException("outer message", new System.Net.WebException("inner message")),
                testLogger);

            // Assert
            testLogger.AssertPartialOutputStringExists("outer message", "inner message");
        }

        [TestMethod]
        public async Task HttpRequestException_OtherInnerException_IsHandledAndOuterMessageLogged()
        {
            // Arrange
            var testLogger = new TestLogger();

            // Act
            await WebServiceHelper.SafeServiceCallAsync<bool>(() =>
                throw new HttpRequestException("outer message", new ArgumentNullException("inner exception")),
                testLogger);

            // Assert
            testLogger.AssertPartialOutputStringExists("outer message");
        }

        [TestMethod]
        public async Task TaskIsCanceled_ExceptionIsNotSuppressed()
        {
            // Arrange
            var testLogger = new TestLogger();
            Func<Task> operation = () => WebServiceHelper.SafeServiceCallAsync(
                () => throw new TaskCanceledException("should not be suppressed"), testLogger);

            // Act
            await operation.Should().ThrowAsync<TaskCanceledException>();

            // Assert
            testLogger.AssertOutputStringExists(CoreStrings.SonarQubeRequestTimeoutOrCancelled);
        }

        [TestMethod]
        public async Task OperationIsCanceled_ExceptionIsNotSuppressed()
        {
            // Arrange
            var testLogger = new TestLogger();
            Func<Task> operation = () => WebServiceHelper.SafeServiceCallAsync(
                () => throw new OperationCanceledException("should not be suppressed"), testLogger);

            // Act
            await operation.Should().ThrowAsync<OperationCanceledException>();

            // Assert
            testLogger.AssertOutputStringExists(CoreStrings.SonarQubeRequestTimeoutOrCancelled);
        }

        [TestMethod]
        public async Task NonCriticalException_IsSuppressed()
        {
            // Arrange
            var testLogger = new TestLogger();

            // Act
            await WebServiceHelper.SafeServiceCallAsync<bool>(() => throw new ArgumentNullException("dummy error message"),
                testLogger);

            // Assert
            testLogger.AssertPartialOutputStringExists("dummy error message");
        }

        [TestMethod]
        public async Task CriticalException_IsNotSuppressed()
        {
            // Arrange
            var testLogger = new TestLogger();
            Func<Task> operation = () => WebServiceHelper.SafeServiceCallAsync(
                () => throw new StackOverflowException("should not be suppressed"), testLogger);

            // Act
            await operation.Should().ThrowAsync<StackOverflowException>();

            // Assert
            testLogger.AssertNoOutputMessages();
        }
    }
}
