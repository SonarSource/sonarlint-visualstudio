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
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

using SonarLint.VisualStudio.Progress.Controller.ErrorNotification;
using Xunit;

namespace SonarLint.VisualStudio.Progress.UnitTests
{
    /// <summary>
    /// Tests for <see cref="VsOutputWindowPaneNotifier"/>
    /// </summary>

    public class VsOutputWindowPaneNotifierTests
    {
        private ConfigurableServiceProvider serviceProvider;
        private Exception expectedException;

        public VsOutputWindowPaneNotifierTests()
        {
            this.serviceProvider = new ConfigurableServiceProvider();
            this.serviceProvider.RegisterService(typeof(SVsTaskSchedulerService), new SingleThreadedTaskSchedulerService());

            this.expectedException = new Exception("VsOutputWindowPaneNotifierTests", new Exception(Environment.TickCount.ToString()));
        }

        #region Tests

        [Fact]
        public void VsOutputWindowPaneNotifier_WithNullServiceProvider_ThrowsArgumentNullException()
        {
            // Arrange
            var outputWindowPane = this.CreateOutputPane(true);

            // Act
            Action act = () => new VsOutputWindowPaneNotifier(null, outputWindowPane, true, "{0}", false);

            // Assert
            act.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("serviceProvider");
        }

        [Fact]
        public void VsOutputWindowPaneNotifier_WithNullPane_ThrowsArgumentNullException()
        {
            // Arrange
            var outputWindowPane = this.CreateOutputPane(true);

            // Act
            Action act = () => new VsOutputWindowPaneNotifier(this.serviceProvider, null, true, "{0}", false);

            // Assert
            act.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("pane");
        }

        [Fact]
        public void VsOutputWindowPaneNotifier_InvalidMessageFormat_ThrowsArgumentNullException()
        {
            // Arrange
            var outputWindowPane = this.CreateOutputPane(true);

            // Act
            Action act1 = () => new VsOutputWindowPaneNotifier(this.serviceProvider, outputWindowPane, false, null, false);
            Action act2 = () => new VsOutputWindowPaneNotifier(this.serviceProvider, outputWindowPane, false, string.Empty, false);
            Action act3 = () => new VsOutputWindowPaneNotifier(this.serviceProvider, outputWindowPane, false, " \t", false);

            // Assert
            act1.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("messageFormat");
            act2.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("messageFormat");
            act3.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("messageFormat");
        }

        [Fact]

        public void Notify_WithException_DoesNotShowOutputWindow()
        {
            // Arrange
            bool logFullException = true;
            bool ensureOutputVisible = false;

            StubVsOutputWindowPane outputPane = this.CreateOutputPane(logFullException);
            StubVsUIShell.StubWindowFrame frame = this.CreateAndRegisterFrame();

            IProgressErrorNotifier testSubject = this.CreateTestSubject(outputPane, ensureOutputVisible, logFullException);

            // Act
            testSubject.Notify(this.expectedException);

            // Assert
            frame.IsShown.Should().BeFalse();
            outputPane.IsActivated.Should().BeFalse("Not expected the output window to be activated");
            outputPane.IsWrittenToOutputWindow.Should().BeTrue("Expected to write to output window");
        }

        [Fact]

        public void Notify_WithFullExceptionAndNoDialog_WritesExceptionToOutputButDoesNotShowErrorDialog()
        {
            // Arrange
            bool logFullException = true;
            bool ensureOutputVisible = false;

            StubVsOutputWindowPane outputPane = this.CreateOutputPane(logFullException);
            StubVsUIShell.StubWindowFrame frame = this.CreateAndRegisterFrame();

            IProgressErrorNotifier testSubject = this.CreateTestSubject(outputPane, ensureOutputVisible, logFullException);

            // Act
            testSubject.Notify(this.expectedException);

            // Assert
            frame.IsShown.Should().BeFalse();
            outputPane.IsActivated.Should().BeFalse("Not expected the output window to be activated");
            outputPane.IsWrittenToOutputWindow.Should().BeTrue("Expected to write to output window");
        }

        [Fact]

        public void Notify_WithFullExceptionAndDialog_WritesExceptionToOutputAndShowsErrorDialog()
        {
            // Arrange
            bool logFullException = true;
            bool ensureOutputVisible = true;

            StubVsOutputWindowPane outputPane = this.CreateOutputPane(logFullException);
            StubVsUIShell.StubWindowFrame frame = this.CreateAndRegisterFrame();

            IProgressErrorNotifier testSubject = this.CreateTestSubject(outputPane, ensureOutputVisible, logFullException);

            // Act
            testSubject.Notify(this.expectedException);

            // Assert
            frame.IsShown.Should().BeTrue();
            outputPane.IsActivated.Should().BeTrue("Expected the output window to be activated");
            outputPane.IsWrittenToOutputWindow.Should().BeTrue("Expected to write to output window");
        }
        #endregion

        #region Helpers

        private string CreateTestMessageFormat()
        {
            return "" + "{0}";
        }

        private VsOutputWindowPaneNotifier CreateTestSubject(IVsOutputWindowPane pane, bool ensureOutputVisible, bool logFullException)
        {
            return new VsOutputWindowPaneNotifier(this.serviceProvider, pane, ensureOutputVisible, this.CreateTestMessageFormat(), logFullException);
        }

        private StubVsUIShell.StubWindowFrame CreateAndRegisterFrame()
        {
            var frame = new StubVsUIShell.StubWindowFrame();

            var uiShell = new StubVsUIShell
            {
                FindToolWindowAction = (windowSlotGuid) =>
                {
                    VSConstants.StandardToolWindows.Output.Should().Be(windowSlotGuid, "Unexpected window slot guid");
                    return frame;
                }
            };

            this.serviceProvider.RegisterService(typeof(SVsUIShell), uiShell);

            return frame;
        }

        private StubVsOutputWindowPane CreateOutputPane(bool logFullException)
        {
            return new StubVsOutputWindowPane
            {
                OutputStringThreadSafeAction = (actualMessage) =>
                {
                    string expectedFormat = this.CreateTestMessageFormat() + Environment.NewLine;
                    actualMessage.Should().Be(string.Format(expectedFormat, logFullException ? expectedException.ToString() : expectedException.Message));
                }
            };
        }

        #endregion
    }
}
