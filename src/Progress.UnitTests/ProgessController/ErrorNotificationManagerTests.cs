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
using Moq;
using SonarLint.VisualStudio.Progress.Controller.ErrorNotification;
using Xunit;

namespace SonarLint.VisualStudio.Progress.UnitTests
{
    /// <summary>
    /// Tests for <see cref="ErrorNotificationMananger"/>
    /// </summary>

    public class ErrorNotificationManagerTests
    {
        #region Tests

        [Fact]
        public void ErrorNotificationMananger_WhenInitialized_DoesNotThrow()
        {
            // Arrange
            var testSubject = new ErrorNotificationManager();
            var testNotifier = new Mock<IProgressErrorNotifier>();

            // Act
            ((IProgressErrorNotifier)testSubject).Notify(new Exception("foo"));

            // Assert
            testNotifier.Verify(x => x.Notify(It.IsAny<Exception>()), Times.Never);
        }

        [Fact]
        public void ErrorNotificationMananger_WhenNotifierAdded_ContainsOneException()
        {
            // Arrange
            var testSubject = new ErrorNotificationManager();
            var testNotifier = new Mock<IProgressErrorNotifier>();

            testSubject.AddNotifier(testNotifier.Object);
            var ex = new Exception("foo");

            // Act
            ((IProgressErrorNotifier)testSubject).Notify(ex);

            // Assert
            testNotifier.Verify(x => x.Notify(ex), Times.Once);
        }

        [Fact]
        public void ErrorNotificationMananger_WhenNotifierRemoved_ContainsNoException()
        {
            // Arrange
            var testSubject = new ErrorNotificationManager();
            var testNotifier = new Mock<IProgressErrorNotifier>();
            testSubject.AddNotifier(testNotifier.Object);
            testSubject.RemoveNotifier(testNotifier.Object);

            var ex = new Exception("foo");


            // Act
            ((IProgressErrorNotifier)testSubject).Notify(ex);

            // Assert
            testNotifier.Verify(x => x.Notify(ex), Times.Never);
        }

        [Fact]
        public void ErrorNotificationMananger_WhenNotifierResetAndAddedAgain_ContainsOneException()
        {
            // Arrange
            var testSubject = new ErrorNotificationManager();
            var testNotifier = new Mock<IProgressErrorNotifier>();

            testSubject.AddNotifier(testNotifier.Object);
            testSubject.RemoveNotifier(testNotifier.Object);
            testSubject.AddNotifier(testNotifier.Object);
            var ex = new Exception("foo");


            // Act
            ((IProgressErrorNotifier)testSubject).Notify(ex);

            // Assert
            testNotifier.Verify(x => x.Notify(ex), Times.Once);
        }

        #endregion
    }
}
