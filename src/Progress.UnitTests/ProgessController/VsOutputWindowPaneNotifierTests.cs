//-----------------------------------------------------------------------
// <copyright file="VsOutputWindowPaneNotifierTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Progress.Controller.ErrorNotification;
using System;

namespace SonarLint.VisualStudio.Progress.UnitTests
{
    /// <summary>
    /// Tests for <see cref="VsOutputWindowPaneNotifier"/>
    /// </summary>
    [TestClass]
    public class VsOutputWindowPaneNotifierTests
    {
        private ConfigurableServiceProvider serviceProvider;
        private Exception expectedException;

        #region Test plumbing

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestInitialize()
        {
            this.serviceProvider = new ConfigurableServiceProvider();
            this.serviceProvider.RegisterService(typeof(SVsTaskSchedulerService), new SingleThreadedTaskSchedulerService());

            this.expectedException = new Exception(this.TestContext.TestName, new Exception(Environment.TickCount.ToString()));
        }

        #endregion

        #region Tests

        [TestMethod]
        [Description("Arg check tests")]
        public void VsOutputWindowPaneNotifier_Args()
        {
            // Setup
            var outputWindowPane = this.CreateOutputPane(true);

            // Act + Verify
            // Test case: 1st argument invalid
            Exceptions.Expect<ArgumentNullException>(() => new VsOutputWindowPaneNotifier(null, outputWindowPane, true, "{0}", false));

            // Test case: 2nd argument invalid
            Exceptions.Expect<ArgumentNullException>(() => new VsOutputWindowPaneNotifier(this.serviceProvider, null, true, "{0}", false));

            // Test case: 4th argument invalid
            Exceptions.Expect<ArgumentNullException>(() => new VsOutputWindowPaneNotifier(this.serviceProvider, outputWindowPane, false, null, false));
            Exceptions.Expect<ArgumentNullException>(() => new VsOutputWindowPaneNotifier(this.serviceProvider, outputWindowPane, false, string.Empty, false));
            Exceptions.Expect<ArgumentNullException>(() => new VsOutputWindowPaneNotifier(this.serviceProvider, outputWindowPane, false, " \t", false));

            // Test case: All valid
            new VsOutputWindowPaneNotifier(this.serviceProvider, outputWindowPane, true, "{0}", false);
            new VsOutputWindowPaneNotifier(this.serviceProvider, outputWindowPane, false, "{0}", true);
        }

        [TestMethod]
        [Description("Verifies notifying of an exception message using an output window")]
        public void VsOutputWindowPaneNotifier_MessageOnly()
        {
            // Setup
            bool logFullException = true;
            bool ensureOutputVisible = false;

            StubVsOutputWindowPane outputPane = this.CreateOutputPane(logFullException);
            StubVsUIShell.StubWindowFrame frame = this.CreateAndRegisterFrame();

            IProgressErrorNotifier testSubject = this.CreateTestSubject(outputPane, ensureOutputVisible, logFullException);

            // Execute
            testSubject.Notify(this.expectedException);

            // Verify
            frame.AssertNotShown();
            outputPane.AssertNotActivated();
            outputPane.AssertWrittenToOutputWindow();
        }

        [TestMethod]
        [Description("Verifies notifying of a full exception using an output window")]
        public void VsOutputWindowPaneNotifier_FullException()
        {
            // Setup
            bool logFullException = true;
            bool ensureOutputVisible = false;

            StubVsOutputWindowPane outputPane = this.CreateOutputPane(logFullException);
            StubVsUIShell.StubWindowFrame frame = this.CreateAndRegisterFrame();

            IProgressErrorNotifier testSubject = this.CreateTestSubject(outputPane, ensureOutputVisible, logFullException);

            // Execute
            testSubject.Notify(this.expectedException);

            // Verify
            frame.AssertNotShown();
            outputPane.AssertNotActivated();
            outputPane.AssertWrittenToOutputWindow();
        }

        [TestMethod]
        [Description("Verifies notifying of a full exception using an output window and ensure that the output is visible")]
        public void VsOutputWindowPaneNotifier_EnsureOutputVisible()
        {
            // Setup
            bool logFullException = true;
            bool ensureOutputVisible = true;

            StubVsOutputWindowPane outputPane = this.CreateOutputPane(logFullException);
            StubVsUIShell.StubWindowFrame frame = this.CreateAndRegisterFrame();

            IProgressErrorNotifier testSubject = this.CreateTestSubject(outputPane, ensureOutputVisible, logFullException);

            // Execute
            testSubject.Notify(this.expectedException);

            // Verify
            frame.AssertShown();
            outputPane.AssertActivated();
            outputPane.AssertWrittenToOutputWindow();
        }
        #endregion

        #region Helpers

        private string CreateTestMessageFormat()
        {
            return this.TestContext.TestName + "{0}";
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
                    Assert.AreEqual(VSConstants.StandardToolWindows.Output, windowSlotGuid, "Unexpected window slot guid");
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
                    MessageVerificationHelper.VerifyNotificationMessage(actualMessage, expectedFormat, this.expectedException, logFullException);
                }
            };
        }

        #endregion
    }
}
