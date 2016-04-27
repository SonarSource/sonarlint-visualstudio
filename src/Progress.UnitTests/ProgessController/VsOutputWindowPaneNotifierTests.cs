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
        private StubVsUIShell uiSHell;
        private VsOutputWindowPaneNotifier testSubject;
        private StubVsOutputWindowPane outputWindowPane;
        private Exception generatedException;

        #region Test plumbing
        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestInitialize()
        {
            this.serviceProvider = new ConfigurableServiceProvider();
            this.uiSHell = new StubVsUIShell();
            this.outputWindowPane = new StubVsOutputWindowPane();
            this.serviceProvider.RegisterService(typeof(SVsTaskSchedulerService), new SingleThreadedTaskSchedulerService());
            this.serviceProvider.RegisterService(typeof(SVsUIShell), this.uiSHell);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            this.outputWindowPane = null;
            this.uiSHell = null;
            this.serviceProvider = null;
            this.testSubject = null;
        }
        #endregion

        #region Tests
        [TestMethod]
        [Description("Arg check tests")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability",
            "S1848:Objects should not be created to be dropped immediately without being used",
            Justification = "Only testing ctor does not throw exceptions; no need to keep resulting instance.",
            Scope = "member",
            Target = "~M:SonarLint.VisualStudio.Progress.UnitTests.VsOutputWindowPaneNotifierTests.VsGeneralOutputWindowNotifier_Args")]
        public void VsOutputWindowPaneNotifier_Args()
        {
            // 1st Argument invalid
            Exceptions.Expect<ArgumentNullException>(() => new VsOutputWindowPaneNotifier(null, this.outputWindowPane, true, "{0}", false));

            // 2nd Argument invalid
            Exceptions.Expect<ArgumentNullException>(() => new VsOutputWindowPaneNotifier(this.serviceProvider, null, true, "{0}", false));

            // 4th Argument invalid
            Exceptions.Expect<ArgumentNullException>(() => new VsOutputWindowPaneNotifier(this.serviceProvider, this.outputWindowPane, false, null, false));
            Exceptions.Expect<ArgumentNullException>(() => new VsOutputWindowPaneNotifier(this.serviceProvider, this.outputWindowPane, false, string.Empty, false));
            Exceptions.Expect<ArgumentNullException>(() => new VsOutputWindowPaneNotifier(this.serviceProvider, this.outputWindowPane, false, " \t", false));

            // Valid
            new VsOutputWindowPaneNotifier(this.serviceProvider, this.outputWindowPane, true, "{0}", false);
            new VsOutputWindowPaneNotifier(this.serviceProvider, this.outputWindowPane, false, "{0}", true);
        }

        [TestMethod]
        [Description("Verifies notifying of an exception message using an output window")]
        public void VsOutputWindowPaneNotifier_MessageOnly()
        {
            // Setup
            bool logWholeMessage = true;
            StubVsUIShell.StubWindowFrame frame = this.Setup(false, logWholeMessage);

            // Execute
            ((IProgressErrorNotifier)this.testSubject).Notify(this.generatedException);

            // Verify
            frame.AssertNotShown();
            this.outputWindowPane.AssertNotActivated();
            this.outputWindowPane.AssertWrittenToOutputWindow();
        }

        [TestMethod]
        [Description("Verifies notifying of a full exception using an output window")]
        public void VsOutputWindowPaneNotifier_FullException()
        {
            // Setup
            bool logWholeMessage = true;
            StubVsUIShell.StubWindowFrame frame = this.Setup(false, logWholeMessage);

            // Execute
            ((IProgressErrorNotifier)this.testSubject).Notify(this.generatedException);

            // Verify
            frame.AssertNotShown();
            this.outputWindowPane.AssertNotActivated();
            this.outputWindowPane.AssertWrittenToOutputWindow();
        }

        [TestMethod]
        [Description("Verifies notifying of a full exception using an output window and ensure that the output is visible")]
        public void VsOutputWindowPaneNotifier_EnsureOutputVisible()
        {
            // Setup
            bool logWholeMessage = true;
            StubVsUIShell.StubWindowFrame frame = this.Setup(true, logWholeMessage);

            // Execute
            ((IProgressErrorNotifier)this.testSubject).Notify(this.generatedException);

            // Verify
            frame.AssertShown();
            this.outputWindowPane.AssertActivated();
            this.outputWindowPane.AssertWrittenToOutputWindow();
        }
        #endregion

        #region Helpers
        
        private StubVsUIShell.StubWindowFrame Setup(bool ensureOutputVisible, bool logWholeMessage)
        {
            string format = this.TestContext.TestName + "{0}";
            this.testSubject = new VsOutputWindowPaneNotifier(this.serviceProvider, this.outputWindowPane, ensureOutputVisible, format, logWholeMessage);
            Exception ex = this.GenerateException();
            StubVsUIShell.StubWindowFrame frame = new StubVsUIShell.StubWindowFrame();
            this.outputWindowPane.OutputStringThreadSafeAction = (actualMessage) =>
            {
                MessageVerificationHelper.VerifyNotificationMessage(actualMessage, format + Environment.NewLine, ex, logWholeMessage);
            };
            this.uiSHell.FindToolWindowAction = (windowSlotGuid) =>
            {
                Assert.AreEqual(VSConstants.StandardToolWindows.Output, windowSlotGuid, "Unexpected window slot guid");
                return frame;
            };
            return frame;
        }

        private Exception GenerateException()
        {
            return this.generatedException = new Exception(this.TestContext.TestName, new Exception(Environment.TickCount.ToString()));
        }
        #endregion
    }
}
