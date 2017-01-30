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
using Microsoft.VisualStudio.Shell.Interop;

using SonarLint.VisualStudio.Progress.Controller.ErrorNotification;
using Xunit;

namespace SonarLint.VisualStudio.Progress.UnitTests
{
    /// <summary>
    /// Tests for <see cref="VsMessageBoxNotifier"/>
    /// </summary>

    public class VsMessageBoxNotifierTests
    {
        private ConfigurableServiceProvider serviceProvider;
        private StubVsUIShell uiSHell;
        private VsMessageBoxNotifier testSubject;

        public VsMessageBoxNotifierTests()
        {
            this.serviceProvider = new ConfigurableServiceProvider();
            this.uiSHell = new StubVsUIShell();
            this.serviceProvider.RegisterService(typeof(SVsTaskSchedulerService), new SingleThreadedTaskSchedulerService());
            this.serviceProvider.RegisterService(typeof(IVsUIShell), this.uiSHell);
        }

        #region Tests

        [Fact]
        public void Ctor_WithNullServiceProvider_ThrowsArgumentNullException()
        {
            // Act
            Action act = () => new VsMessageBoxNotifier(null, "title", "{0}", false);

            // Assert
            act.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("serviceProvider");
        }

        [Fact]
        public void Ctor_WithNullTitle_ThrowsArgumentNullException()
        {
            // Act
            Action act = () => new VsMessageBoxNotifier(this.serviceProvider, null, "{0}", false);

            // Assert
            act.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("title");
        }

        [Fact]
        public void Ctor_WithInvalidMessageFormat_ThrowsArgumentNullException()
        {
            // Act
            Action act1 = () => new VsMessageBoxNotifier(this.serviceProvider, "title", null, false);
            Action act2 = () => new VsMessageBoxNotifier(this.serviceProvider, "title", string.Empty, false);
            Action act3 = () => new VsMessageBoxNotifier(this.serviceProvider, "title", " \t", false);

            // Assert
            act1.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("messageFormat");
            act2.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("messageFormat");
            act3.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("messageFormat");
        }

        [Fact]
        public void Notify_WithAnException_DisplaysTheMessageBox()
        {
            // Arrange
            bool logWholeMessage = false;
            Exception ex = this.Setup(logWholeMessage);

            // Act
            ((IProgressErrorNotifier)this.testSubject).Notify(ex);

            // Assert
            this.uiSHell.IsMessageBoxShown.Should().BeTrue();
        }

        [Fact]

        public void Notify_WithFullException_DisplaysTheMessageBox()
        {
            // Arrange
            bool logWholeMessage = true;
            Exception ex = this.Setup(logWholeMessage);

            // Act
            ((IProgressErrorNotifier)this.testSubject).Notify(ex);

            // Assert
            this.uiSHell.IsMessageBoxShown.Should().BeTrue();
        }
        #endregion

        #region Helpers

        private Exception Setup(bool logWholeMessage)
        {
            string format = "" + "{0}";
            string title = "";
            this.testSubject = new VsMessageBoxNotifier(this.serviceProvider, title, format, logWholeMessage);
            Exception ex = new Exception("", new Exception(Environment.TickCount.ToString()));
            this.uiSHell.ShowMessageBoxAction = (actualTitle, actualMessage) =>
            {
                title.Should().Be(actualTitle);
                actualMessage.Should().Be(string.Format(format, logWholeMessage ? ex.ToString() : ex.Message));
            };
            return ex;
        }

        #endregion
    }
}
