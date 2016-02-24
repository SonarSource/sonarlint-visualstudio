//-----------------------------------------------------------------------
// <copyright file="VsActivityLogNotifierTests.cs" company="SonarSource SA and Microsoft Corporation">
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
    /// Tests for <see cref="VsActivityLogNotifier"/>
    /// </summary>
    [TestClass]
    public class VsActivityLogNotifierTests
    {
        private ConfigurableServiceProvider serviceProvider;
        private StubVsActivityLog activityLog;
        private VsActivityLogNotifier testSubject;

        #region Test plumbing
        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestInitialize()
        {
            this.serviceProvider = new ConfigurableServiceProvider();
            this.activityLog = new StubVsActivityLog();
            this.serviceProvider.RegisterService(typeof(SVsActivityLog), this.activityLog);
            this.serviceProvider.RegisterService(typeof(SVsTaskSchedulerService), new SingleThreadedTaskSchedulerService());
        }

        [TestCleanup]
        public void TestCleanup()
        {
            this.activityLog = null;
            this.serviceProvider = null;
            this.testSubject = null;
        }
        #endregion

        #region Tests
        [TestMethod]
        [Description("Arg check tests")]
        public void VsActivityLogNotifier_Args()
        {
            // 1st Argument invalid
            Exceptions.Expect<ArgumentNullException>(() => new VsActivityLogNotifier(null, "source", "{0}", false));

            // 2nd Argument invalid
            Exceptions.Expect<ArgumentNullException>(() => new VsActivityLogNotifier(this.serviceProvider, null, "{0}", false));
            Exceptions.Expect<ArgumentNullException>(() => new VsActivityLogNotifier(this.serviceProvider, string.Empty, "{0}", false));
            Exceptions.Expect<ArgumentNullException>(() => new VsActivityLogNotifier(this.serviceProvider, " \t", "{0}", false));

            // 3rd Argument invalid
            Exceptions.Expect<ArgumentNullException>(() => new VsActivityLogNotifier(this.serviceProvider, "source", null, false));
            Exceptions.Expect<ArgumentNullException>(() => new VsActivityLogNotifier(this.serviceProvider, "source", string.Empty, false));
            Exceptions.Expect<ArgumentNullException>(() => new VsActivityLogNotifier(this.serviceProvider, "source", " \t", false));

            // Valid
            new VsActivityLogNotifier(this.serviceProvider, "source", "{0}", false);
            new VsActivityLogNotifier(this.serviceProvider, "source", "{0}", true);
        }

        [TestMethod]
        [Description("Verifies logging of an exception message in activity log")]
        public void VsActivityLogNotifier_MessageOnly()
        {
            // Setup
            bool logWholeMessage = true;
            Exception ex = this.Setup(logWholeMessage);

            // Execute
            ((IProgressErrorNotifier)this.testSubject).Notify(ex);

            // Verify
            this.activityLog.AssertEntryLogged();
        }

        [TestMethod]
        [Description("Verifies logging of a full exception in activity log")]
        public void VsActivityLogNotifier_FullException()
        {
            // Setup
            bool logWholeMessage = true;
            Exception ex = this.Setup(logWholeMessage);

            // Execute
            ((IProgressErrorNotifier)this.testSubject).Notify(ex);

            // Verify
            this.activityLog.AssertEntryLogged();
        }
        #endregion

        #region Helpers
      
        private Exception Setup(bool logWholeMessage)
        {
            string format = this.TestContext.TestName + "{0}";
            string source = this.TestContext.TestName;
            this.testSubject = new VsActivityLogNotifier(this.serviceProvider, source, format, logWholeMessage);
            Exception ex = this.GenerateException();
            this.activityLog.LogEntryAction = (entryType, actualSource, actualMessage) =>
            {
                Assert.AreEqual((uint)__ACTIVITYLOG_ENTRYTYPE.ALE_ERROR, entryType, "Unexpected entry type");
                Assert.AreEqual(source, actualSource, "Unexpected entry source");
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
