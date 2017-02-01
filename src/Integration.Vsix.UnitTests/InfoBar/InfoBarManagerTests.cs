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
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.InfoBar;
using SonarLint.VisualStudio.Integration.Vsix.InfoBar;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class InfoBarManagerTests
    {
        private ConfigurableServiceProvider serviceProvider;
        private ConfigurableVsUIShell shell;

        [TestInitialize]
        public void TestInit()
        {
            this.serviceProvider = new ConfigurableServiceProvider();

            this.shell = new ConfigurableVsUIShell();
            this.serviceProvider.RegisterService(typeof(SVsUIShell), this.shell);
        }

        #region Tests

        [TestMethod]
        public void InfoBarManager_ArgChecks()
        {
            Exceptions.Expect<ArgumentNullException>(() => new InfoBarManager(null));
        }

        [TestMethod]
        public void InfoBarManager_AttachInfoBar_ArgChecks()
        {
            // Arrange
            var testSubject = new InfoBarManager(this.serviceProvider);

            // Simple checks
            Exceptions.Expect<ArgumentNullException>(() => testSubject.AttachInfoBar(Guid.Empty, null, "button text", KnownMonikers.EventError));
            Exceptions.Expect<ArgumentNullException>(() => testSubject.AttachInfoBar(Guid.Empty, "", "button text", KnownMonikers.EventError));
            Exceptions.Expect<ArgumentNullException>(() => testSubject.AttachInfoBar(Guid.Empty, "message", null, KnownMonikers.EventError));
            Exceptions.Expect<ArgumentNullException>(() => testSubject.AttachInfoBar(Guid.Empty, "message", " ", KnownMonikers.EventError));

            // Actually checking if the frame exists
            Exceptions.Expect<ArgumentException>(() => testSubject.AttachInfoBar(Guid.Empty, "message", "button text", KnownMonikers.EventError));
        }

        [TestMethod]
        public void InfoBarManager_AttachInfoBar()
        {
            // Arrange
            Guid windowGuid = new Guid();
            ConfigurableVsWindowFrame frame = this.shell.RegisterToolWindow(windowGuid);
            this.serviceProvider.RegisterService(typeof(SVsInfoBarUIFactory), new ConfigurableVsInfoBarUIFactory());
            var testSubject = new InfoBarManager(this.serviceProvider);
            ConfigurableVsInfoBarHost host = RegisterFrameInfoBarHost(frame);

            // Sanity
            host.AssertInfoBars(0);

            // Act
            IInfoBar infoBarWrapper = testSubject.AttachInfoBar(windowGuid, "Hello", "world", KnownMonikers.UserWarning);
            frame.ShowNoActivateCalledCount.Should().Be(1);
            bool actionClicked = false;
            bool closed = false;
            infoBarWrapper.ButtonClick += (s, e) => actionClicked = true;
            infoBarWrapper.Closed += (s, e) => closed = true;

            // Assert
            infoBarWrapper.Should().NotBeNull();
            host.AssertInfoBars(1);
            var infoBarUI = host.MockedElements.Single();
            infoBarUI.Model.TextSpans.Count.Should().Be(1);
            infoBarUI.Model.TextSpans.GetSpan(0).Text.Should().Be("Hello");
            infoBarUI.Model.ActionItems.Count.Should().Be(1);
            infoBarUI.Model.ActionItems.GetItem(0).Text.Should().Be("world");

            // Sanity
            actionClicked.Should().BeFalse();
            closed.Should().BeFalse();

            // Act (check if close event is fired)
            infoBarUI.SimulateClickEvent();

            // Assert
            actionClicked.Should().BeTrue();
            closed.Should().BeFalse();

            // Act (check if close event is fired)
            infoBarUI.SimulateClosedEvent();

            // Assert
            closed.Should().BeTrue();

            // Act (check that events won't fire once closed)
            actionClicked = false;
            closed = false;
            infoBarUI.SimulateClickEvent();
            infoBarWrapper.Close();
            infoBarUI.SimulateClosedEvent();

            // Assert
            actionClicked.Should().BeFalse();
            closed.Should().BeFalse();
            frame.ShowNoActivateCalledCount.Should().Be(1); // Should only be called once in all this flow
        }

        [TestMethod]
        public void InfoBarManager_AttachInfoBar_Failures()
        {
            // Arrange
            Guid windowGuid = new Guid();
            ConfigurableVsWindowFrame frame = this.shell.RegisterToolWindow(windowGuid);
            var testSubject = new InfoBarManager(this.serviceProvider);

            // Case 1: No service
            this.serviceProvider.AssertOnUnexpectedServiceRequest = false;

            // Act + Assert
            testSubject.AttachInfoBar(windowGuid, "Hello", "world", default(ImageMoniker)).Should().BeNull();
            frame.ShowNoActivateCalledCount.Should().Be(0);

            // Case 2: Service exists, no host for frame
            this.serviceProvider.RegisterService(typeof(SVsInfoBarUIFactory), new ConfigurableVsInfoBarUIFactory());

            // Act + Assert
            testSubject.AttachInfoBar(windowGuid, "Hello", "world", default(ImageMoniker)).Should().BeNull();
            frame.ShowNoActivateCalledCount.Should().Be(0);
        }

        [TestMethod]
        public void InfoBarManager_DetachInfoBar_ArgChecks()
        {
            // Arrange
            var testSubject = new InfoBarManager(this.serviceProvider);

            Exceptions.Expect<ArgumentNullException>(() => testSubject.DetachInfoBar(null));
            Exceptions.Expect<ArgumentException>(() => testSubject.DetachInfoBar(new InvalidInfoBar()));
        }

        [TestMethod]
        public void InfoBarManager_DetachInfoBar()
        {
            // Arrange
            Guid windowGuid = new Guid();
            ConfigurableVsWindowFrame frame = this.shell.RegisterToolWindow(windowGuid);
            this.serviceProvider.RegisterService(typeof(SVsInfoBarUIFactory), new ConfigurableVsInfoBarUIFactory());
            var testSubject = new InfoBarManager(this.serviceProvider);
            ConfigurableVsInfoBarHost host = RegisterFrameInfoBarHost(frame);
            IInfoBar infoBarWrapper = testSubject.AttachInfoBar(windowGuid, "Hello", "world", default(ImageMoniker));
            bool closed = false;
            infoBarWrapper.Closed += (s, e) => closed = true;

            // Sanity
            host.AssertInfoBars(1);

            // Act
            testSubject.DetachInfoBar(infoBarWrapper);

            // Assert
            closed.Should().BeTrue("Expected to auto-close");
            host.AssertInfoBars(0);
        }

        #endregion Tests

        #region Test helpers

        private static ConfigurableVsInfoBarHost RegisterFrameInfoBarHost(ConfigurableVsWindowFrame frame)
        {
            var host = new ConfigurableVsInfoBarHost();
            frame.RegisterProperty((int)__VSFPROPID7.VSFPROPID_InfoBarHost, host);
            return host;
        }

        private class InvalidInfoBar : IInfoBar
        {
            event EventHandler IInfoBar.ButtonClick
            {
                add
                {
                    throw new NotImplementedException();
                }

                remove
                {
                    throw new NotImplementedException();
                }
            }

            event EventHandler IInfoBar.Closed
            {
                add
                {
                    throw new NotImplementedException();
                }

                remove
                {
                    throw new NotImplementedException();
                }
            }

            void IInfoBar.Close()
            {
                throw new NotImplementedException();
            }
        }

        #endregion Test helpers
    }
}