//-----------------------------------------------------------------------
// <copyright file="VsMessageBoxNotifierTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarLint.VisualStudio.Progress.UnitTests;
using SonarLint.VisualStudio.Progress.Controller.ErrorNotification;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Globalization;

namespace SonarLint.VisualStudio.Progress.UnitTests
{
    /// <summary>
    /// Tests for <see cref="VsMessageBoxNotifier"/>
    /// </summary>
    [TestClass]
    public class VsMessageBoxNotifierTests
    {
        private ConfigurableServiceProvider serviceProvider;
        private StubVsUIShell uiSHell;
        private VsMessageBoxNotifier testSubject;

        #region Test plumbing
        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestInitialize()
        {
            this.serviceProvider = new ConfigurableServiceProvider();
            this.uiSHell = new StubVsUIShell();
            this.serviceProvider.RegisterService(typeof(SVsTaskSchedulerService), new SingleThreadedTaskSchedulerService());
            this.serviceProvider.RegisterService(typeof(IVsUIShell), this.uiSHell);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            this.uiSHell = null;
            this.serviceProvider = null;
            this.testSubject = null;
        }
        #endregion

        #region Tests
        [TestMethod]
        [Description("Arg check tests")]
        public void VsMessageBoxNotifier_Args()
        {
            // 1st Argument invalid
            Exceptions.Expect<ArgumentNullException>(() => new VsMessageBoxNotifier(null, "title", "{0}", false));

            // 2nd Argument invalid
            Exceptions.Expect<ArgumentNullException>(() => new VsMessageBoxNotifier(this.serviceProvider, null, "{0}", false));

            // 3rd Argument invalid
            Exceptions.Expect<ArgumentNullException>(() => new VsMessageBoxNotifier(this.serviceProvider, "title", null, false));
            Exceptions.Expect<ArgumentNullException>(() => new VsMessageBoxNotifier(this.serviceProvider, "title", string.Empty, false));
            Exceptions.Expect<ArgumentNullException>(() => new VsMessageBoxNotifier(this.serviceProvider, "title", " \t", false));

            // Valid
            new VsMessageBoxNotifier(this.serviceProvider, string.Empty, "{0}", false);
            new VsMessageBoxNotifier(this.serviceProvider, "\t", "{0}", true);
        }

        [TestMethod]
        [Description("Verifies notifying of an exception message using a message box")]
        public void VsMessageBoxNotifier_MessageOnly()
        {
            // Setup
            bool logWholeMessage = true;
            Exception ex = this.Setup(logWholeMessage);

            // Execute
            ((IProgressErrorNotifier)this.testSubject).Notify(ex);

            // Verify
            this.uiSHell.AssertMessageBoxShown();
        }

        [TestMethod]
        [Description("Verifies notifying of a full exception using a message box")]
        public void VsMessageBoxNotifier_FullException()
        {
            // Setup
            bool logWholeMessage = true;
            Exception ex = this.Setup(logWholeMessage);

            // Execute
            ((IProgressErrorNotifier)this.testSubject).Notify(ex);

            // Verify
            this.uiSHell.AssertMessageBoxShown();
        }
        #endregion

        #region Helpers

        private Exception Setup(bool logWholeMessage)
        {
            string format = this.TestContext.TestName + "{0}";
            string title = this.TestContext.TestName;
            this.testSubject = new VsMessageBoxNotifier(this.serviceProvider, title, format, logWholeMessage);
            Exception ex = this.GenerateException();
            this.uiSHell.ShowMessageBoxAction = (actualTitle, actualMessage) =>
            {
                Assert.AreEqual(title, actualTitle, "Unexpected message box title");
                MessageVerificationHelper.VerifyNotificationMessage(actualMessage, format, ex, logWholeMessage);
            };
            return ex;
        }

        private Exception GenerateException()
        {
            return new Exception(this.TestContext.TestName, new Exception(Environment.TickCount.ToString()));
        }
        #endregion
    }
}
