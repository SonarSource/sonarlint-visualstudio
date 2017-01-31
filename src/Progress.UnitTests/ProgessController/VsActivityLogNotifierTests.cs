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
    /// Tests for <see cref="VsActivityLogNotifier"/>
    /// </summary>

    public class VsActivityLogNotifierTests
    {
        private ConfigurableServiceProvider serviceProvider;
        private StubVsActivityLog activityLog;
        private VsActivityLogNotifier testSubject;

        public VsActivityLogNotifierTests()
        {
            this.serviceProvider = new ConfigurableServiceProvider();
            this.activityLog = new StubVsActivityLog();
            this.serviceProvider.RegisterService(typeof(SVsActivityLog), this.activityLog);
            this.serviceProvider.RegisterService(typeof(SVsTaskSchedulerService), new SingleThreadedTaskSchedulerService());
        }

        #region Tests

        [Fact]
        public void Ctor_WithNullServiceProvider_ThrowsArgumentNullException()
        {
            // Arrange & Act
            Action act = () => new VsActivityLogNotifier(null, "source", "{0}", false);

            // Assert
            act.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("serviceProvider");
        }

        [Fact]
        public void Ctor_WithInvalidEntrySource_ThrowsArgumentNullException()
        {
            // Arrange & Act
            Action act1 = () => new VsActivityLogNotifier(this.serviceProvider, null, "{0}", false);
            Action act2 = () => new VsActivityLogNotifier(this.serviceProvider, string.Empty, "{0}", false);
            Action act3 = () => new VsActivityLogNotifier(this.serviceProvider, " \t", "{0}", false);


            // Assert
            act1.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("entrySource");
            act2.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("entrySource");
            act3.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("entrySource");
        }

        [Fact]
        public void Ctor_WithInvalidMessageFormat_ThrowsArgumentNullException()
        {
            // Arrange & Act
            Action act1 = () => new VsActivityLogNotifier(this.serviceProvider, "source", null, false);
            Action act2 = () => new VsActivityLogNotifier(this.serviceProvider, "source", string.Empty, false);
            Action act3 = () => new VsActivityLogNotifier(this.serviceProvider, "source", " \t", false);


            // Assert
            act1.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("messageFormat");
            act2.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("messageFormat");
            act3.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("messageFormat");
        }

        [Fact]

        public void Notify_WithException_LogsException()
        {
            // Arrange
            bool logWholeMessage = false;
            Exception ex = this.Setup(logWholeMessage);

            // Act
            ((IProgressErrorNotifier)this.testSubject).Notify(ex);

            // Assert
            this.activityLog.HasLoggedEntry.Should().BeTrue();
        }

        [Fact]

        public void Notify_WithFullException_LogsFullException()
        {
            // Arrange
            bool logWholeMessage = true;
            Exception ex = this.Setup(logWholeMessage);

            // Act
            ((IProgressErrorNotifier)this.testSubject).Notify(ex);

            // Assert
            this.activityLog.HasLoggedEntry.Should().BeTrue();
        }
        #endregion

        #region Helpers

        private Exception Setup(bool logWholeMessage)
        {
            string format = "" + "{0}";
            string source = "";
            this.testSubject = new VsActivityLogNotifier(this.serviceProvider, source, format, logWholeMessage);
            Exception ex = this.GenerateException();
            this.activityLog.LogEntryAction = (entryType, actualSource, actualMessage) =>
            {
                entryType.Should().Be((uint)__ACTIVITYLOG_ENTRYTYPE.ALE_ERROR, "Unexpected entry type");
                source.Should().Be(actualSource, "Unexpected entry source");
                actualMessage.Should().Be(string.Format(format, logWholeMessage ? ex.ToString() : ex.Message));
            };
            return ex;
        }

        private Exception GenerateException()
        {
            return new Exception("", new Exception(Environment.TickCount.ToString()));
        }
        #endregion
    }
}
