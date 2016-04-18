//-----------------------------------------------------------------------
// <copyright file="InfoBarManagerTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.InfoBar;
using SonarLint.VisualStudio.Integration.Vsix.InfoBar;
using System;
using System.Linq;

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
            // Setup
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
            // Setup
            Guid windowGuid = new Guid();
            ConfigurableVsWindowFrame frame = this.shell.RegisterToolWindow(windowGuid);
            this.serviceProvider.RegisterService(typeof(SVsInfoBarUIFactory), new ConfigurableVsInfoBarUIFactory());
            var testSubject = new InfoBarManager(this.serviceProvider);
            ConfigurableVsInfoBarHost host = RegisterFrameInfoBarHost(frame);

            // Sanity
            host.AssertInfoBars(0);

            // Act
            IInfoBar infoBarWrapper = testSubject.AttachInfoBar(windowGuid, "Hello", "world", KnownMonikers.UserWarning);
            bool actionClicked = false;
            bool closed = false;
            infoBarWrapper.ButtonClick += (s, e) => actionClicked = true;
            infoBarWrapper.Closed += (s, e) => closed = true;

            // Verify
            Assert.IsNotNull(infoBarWrapper);
            host.AssertInfoBars(1);
            var infoBarUI = host.MockedElements.Single();
            Assert.AreEqual(1, infoBarUI.Model.TextSpans.Count);
            Assert.AreEqual("Hello", infoBarUI.Model.TextSpans.GetSpan(0).Text);
            Assert.AreEqual(1, infoBarUI.Model.ActionItems.Count);
            Assert.AreEqual("world", infoBarUI.Model.ActionItems.GetItem(0).Text);

            // Sanity
            Assert.IsFalse(actionClicked);
            Assert.IsFalse(closed);

            // Act (check if close event is fired)
            infoBarUI.SimulateClickEvent();

            // Verify
            Assert.IsTrue(actionClicked);
            Assert.IsFalse(closed);

            // Act (check if close event is fired)
            infoBarUI.SimulateClosedEvent();

            // Verify
            Assert.IsTrue(closed);

            // Act (check that events won't fire once closed)
            actionClicked = false;
            closed = false;
            infoBarUI.SimulateClickEvent();
            infoBarWrapper.Close();
            infoBarUI.SimulateClosedEvent();

            // Verify
            Assert.IsFalse(actionClicked);
            Assert.IsFalse(closed);
        }

        [TestMethod]
        public void InfoBarManager_AttachInfoBar_Failures()
        {
            // Setup
            Guid windowGuid = new Guid();
            this.shell.RegisterToolWindow(windowGuid);
            var testSubject = new InfoBarManager(this.serviceProvider);

            // Case 1: No service
            this.serviceProvider.AssertOnUnexpectedServiceRequest = false;

            // Act + Verify
            Assert.IsNull(testSubject.AttachInfoBar(windowGuid, "Hello", "world", default(ImageMoniker)));

            // Case 2: Service exists, no host for frame
            this.serviceProvider.RegisterService(typeof(SVsInfoBarUIFactory), new ConfigurableVsInfoBarUIFactory());

            // Act + Verify
            Assert.IsNull(testSubject.AttachInfoBar(windowGuid, "Hello", "world", default(ImageMoniker)));
        }

        [TestMethod]
        public void InfoBarManager_DetachInfoBar_ArgChecks()
        {
            // Setup
            var testSubject = new InfoBarManager(this.serviceProvider);

            Exceptions.Expect<ArgumentNullException>(() => testSubject.DetachInfoBar(null));
            Exceptions.Expect<ArgumentException>(() => testSubject.DetachInfoBar(new InvalidInfoBar()));
        }

        [TestMethod]
        public void InfoBarManager_DetachInfoBar()
        {
            // Setup
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

            // Verify
            Assert.IsTrue(closed, "Expected to auto-close");
            host.AssertInfoBars(0);
        }
        #endregion

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
        #endregion
    }
}
