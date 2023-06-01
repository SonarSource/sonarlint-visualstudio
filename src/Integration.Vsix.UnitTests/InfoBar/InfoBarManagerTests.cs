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
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.InfoBar;
using SonarLint.VisualStudio.Integration.Vsix.InfoBar;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class InfoBarManagerTests
    {
        private readonly Guid dummyWindowGuid = Guid.NewGuid();

        private ConfigurableServiceProvider serviceProvider;
        private ConfigurableVsUIShell shell;

        [TestInitialize]
        public void TestInit()
        {
            ThreadHelper.SetCurrentThreadAsUIThread();
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
            Exceptions.Expect<ArgumentNullException>(() => testSubject.AttachInfoBar(Guid.Empty, null, CreateFromVsMoniker(KnownMonikers.EventError)));
            Exceptions.Expect<ArgumentNullException>(() => testSubject.AttachInfoBar(Guid.Empty, "", CreateFromVsMoniker(KnownMonikers.EventError)));

            // Actually checking if the frame exists
            Exceptions.Expect<ArgumentException>(() => testSubject.AttachInfoBarWithButton(Guid.Empty, "message", "button text", CreateFromVsMoniker(KnownMonikers.EventError)));
        }

        [TestMethod]
        public void InfoBarManager_AttachInfoBarWithButton_ArgChecks()
        {
            // Arrange
            var testSubject = new InfoBarManager(this.serviceProvider);

            // Simple checks
            Exceptions.Expect<ArgumentNullException>(() => testSubject.AttachInfoBarWithButton(Guid.Empty, null, "button text", CreateFromVsMoniker(KnownMonikers.EventError)));
            Exceptions.Expect<ArgumentNullException>(() => testSubject.AttachInfoBarWithButton(Guid.Empty, "", "button text", CreateFromVsMoniker(KnownMonikers.EventError)));
            Exceptions.Expect<ArgumentNullException>(() => testSubject.AttachInfoBarWithButton(Guid.Empty, "message", null, CreateFromVsMoniker(KnownMonikers.EventError)));
            Exceptions.Expect<ArgumentNullException>(() => testSubject.AttachInfoBarWithButton(Guid.Empty, "message", " ", CreateFromVsMoniker(KnownMonikers.EventError)));

            // Actually checking if the frame exists
            Exceptions.Expect<ArgumentException>(() => testSubject.AttachInfoBarWithButton(Guid.Empty, "message", "button text", CreateFromVsMoniker(KnownMonikers.EventError)));
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
            IInfoBar infoBarWrapper = testSubject.AttachInfoBarWithButton(windowGuid, "Hello", "world", CreateFromVsMoniker(KnownMonikers.UserWarning));
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
            infoBarUI.Model.ActionItems.GetItem(0).IsButton.Should().BeTrue();

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
            testSubject.AttachInfoBarWithButton(windowGuid, "Hello", "world", default).Should().BeNull();
            frame.ShowNoActivateCalledCount.Should().Be(0);

            // Case 2: Service exists, no host for frame
            this.serviceProvider.RegisterService(typeof(SVsInfoBarUIFactory), new ConfigurableVsInfoBarUIFactory());

            // Act + Assert
            testSubject.AttachInfoBarWithButton(windowGuid, "Hello", "world", default).Should().BeNull();
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
            IInfoBar infoBarWrapper = testSubject.AttachInfoBarWithButton(windowGuid, "Hello", "world", default);
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

        [TestMethod]
        public void AttachInfoBarWithButtons_NoMessage_ArgumentNullException()
        {
            var testSubject = CreateTestSubject();

            Action act = () => testSubject.AttachInfoBarWithButtons(dummyWindowGuid, null, new[] {"button"}, default);

            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("message");
        }

        [TestMethod]
        public void AttachInfoBarWithButtons_NoButtons_ArgumentNullException()
        {
            var testSubject = CreateTestSubject();

            Action act = () => testSubject.AttachInfoBarWithButtons(dummyWindowGuid, "message", null, default);

            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("buttonTexts");
        }

        [TestMethod]
        public void AttachInfoBarWithButtons_CreatedInfoBarWithMultipleButtons()
        {
            var vsUiShell = new ConfigurableVsUIShell();
            var frame = vsUiShell.RegisterToolWindow(dummyWindowGuid);
            var host = RegisterFrameInfoBarHost(frame);

            var testSubject = CreateTestSubject(vsUiShell);

            var buttons = new[]
            {
                "button1",
                "button2",
                "button3"
            };

            var infoBar = testSubject.AttachInfoBarWithButtons(dummyWindowGuid, "message", buttons, default);

            infoBar.Should().NotBeNull();

            host.AssertInfoBars(1);

            var infoBarUI = host.MockedElements.SingleOrDefault();

            infoBarUI.Should().NotBeNull();

            infoBarUI.Model.TextSpans.Count.Should().Be(1);
            infoBarUI.Model.TextSpans.GetSpan(0).Text.Should().Be("message");

            infoBarUI.Model.ActionItems.Count.Should().Be(3);

            infoBarUI.Model.ActionItems.GetItem(0).Text.Should().Be("button1");
            infoBarUI.Model.ActionItems.GetItem(1).Text.Should().Be("button2");
            infoBarUI.Model.ActionItems.GetItem(2).Text.Should().Be("button3");

            infoBarUI.Model.ActionItems.GetItem(0).IsButton.Should().BeFalse();
            infoBarUI.Model.ActionItems.GetItem(1).IsButton.Should().BeFalse();
            infoBarUI.Model.ActionItems.GetItem(2).IsButton.Should().BeFalse();
        }

        [TestMethod]
        public void AttachInfoBarWithButtons_ButtonClickedEvent_RaisedWithTheRightButtonText()
        {
            var vsUiShell = new ConfigurableVsUIShell();
            var frame = vsUiShell.RegisterToolWindow(dummyWindowGuid);
            var host = RegisterFrameInfoBarHost(frame);

            var testSubject = CreateTestSubject(vsUiShell);

            var buttons = new[]
            {
                "button1",
                "button2",
                "button3"
            };

            var infoBar = testSubject.AttachInfoBarWithButtons(dummyWindowGuid, "message", buttons, default);

            infoBar.Should().NotBeNull();

            var eventHandler = new Mock<Action<InfoBarButtonClickedEventArgs>>();
            infoBar.ButtonClick += (_, args) => eventHandler.Object(args);

            var infoBarUI = host.MockedElements.Single();

            infoBarUI.SimulateClickEvent(infoBarUI.Model.ActionItems.GetItem(1));
            eventHandler.Verify(x => x(It.Is((InfoBarButtonClickedEventArgs e) => e.ClickedButtonText == "button2")), Times.Once);
            eventHandler.VerifyNoOtherCalls();

            eventHandler.Reset();

            infoBarUI.SimulateClickEvent(infoBarUI.Model.ActionItems.GetItem(2));
            eventHandler.Verify(x => x(It.Is((InfoBarButtonClickedEventArgs e) => e.ClickedButtonText == "button3")), Times.Once);
            eventHandler.VerifyNoOtherCalls();
        }

        #region MainWindow tests
        // The ToolWindow tests above cover the common implementation of gold bar functionality.
        // These MainWindow tests just cover a few extra main window specific cases.

        [TestMethod]
        public void InfoBarManager_AttachInfoBarToMainWindowAndDetach_ArgChecks()
        {
            var testSubject = CreateTestSubject();

            Action act = () => testSubject.AttachInfoBarToMainWindow(null, SonarLintImageMoniker.OfficialSonarLintMoniker);
            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("message");

            act = () => testSubject.AttachInfoBarToMainWindow("a message", SonarLintImageMoniker.OfficialSonarLintMoniker, null);
            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("buttonTexts");
        }

        [TestMethod]
        public void InfoBarManager_AttachInfoBarToMainWindowAndDetach_AttachAndDetach()
        {
            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock.Setup(x => x.GetService(typeof(SVsInfoBarUIFactory))).Returns(new ConfigurableVsInfoBarUIFactory());

            var host = new Mock<IVsInfoBarHost>();
            RegisterMainWindowHostWithShell(serviceProviderMock, host.Object);

            var testSubject = new InfoBarManager(serviceProviderMock.Object);
            host.Verify(x => x.AddInfoBar(It.IsAny<IVsInfoBarUIElement>()), Times.Never);

            var infoBar = testSubject.AttachInfoBarToMainWindow("message", default);
            infoBar.Should().NotBeNull();

            serviceProviderMock.Verify(x => x.GetService(typeof(SVsInfoBarUIFactory)), Times.Once);
            host.Verify(x => x.AddInfoBar(It.IsAny<IVsInfoBarUIElement>()), Times.Once);

            testSubject.DetachInfoBar(infoBar);
            host.Verify(x => x.RemoveInfoBar(It.IsAny<IVsInfoBarUIElement>()), Times.Once);
        }

        [TestMethod]
        public void InfoBarManager_AttachInfoBarToMainWindow_HostIsNotAvailable_ReturnsNull()
        {
            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock.Setup(x => x.GetService(typeof(SVsInfoBarUIFactory))).Returns(new ConfigurableVsInfoBarUIFactory());

            RegisterMainWindowHostWithShell(serviceProviderMock, null);

            var testSubject = new InfoBarManager(serviceProviderMock.Object);

            // infoBarUIFactory not called
            var actual = testSubject.AttachInfoBarToMainWindow("message", default);
            actual.Should().BeNull();
            serviceProviderMock.Verify(x => x.GetService(typeof(SVsInfoBarUIFactory)), Times.Never);
        }

        #endregion

        #endregion Tests

        #region Test helpers

        private static InfoBarManager CreateTestSubject(ConfigurableVsUIShell vsUiShell = null)
        {
            vsUiShell ??= new ConfigurableVsUIShell();

            var serviceProviderMock = new ConfigurableServiceProvider();
            serviceProviderMock.RegisterService(typeof(SVsUIShell), vsUiShell);
            serviceProviderMock.RegisterService(typeof(SVsInfoBarUIFactory), new ConfigurableVsInfoBarUIFactory());

            return new InfoBarManager(serviceProviderMock);
        }

        private static SonarLintImageMoniker CreateFromVsMoniker(ImageMoniker imageMoniker) => new SonarLintImageMoniker(imageMoniker.Guid, imageMoniker.Id);

        private static ConfigurableVsInfoBarHost RegisterFrameInfoBarHost(ConfigurableVsWindowFrame frame)
        {
            var host = new ConfigurableVsInfoBarHost();
            frame.RegisterProperty((int)__VSFPROPID7.VSFPROPID_InfoBarHost, host);
            return host;
        }

        private static void RegisterMainWindowHostWithShell(Mock<IServiceProvider> serviceProvider, IVsInfoBarHost host)
        {
            var hostAsObject = (object)host;

            var shellMock = new Mock<IVsShell>();
            shellMock.Setup(x => x.GetProperty((int)__VSSPROPID7.VSSPROPID_MainWindowInfoBarHost, out hostAsObject));

            serviceProvider.Setup(x => x.GetService(typeof(SVsShell))).Returns(shellMock.Object);
        }

        private class InvalidInfoBar : IInfoBar
        {
            event EventHandler<InfoBarButtonClickedEventArgs> IInfoBar.ButtonClick
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
