//-----------------------------------------------------------------------
// <copyright file="ErrorNotificationManagerTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarLint.VisualStudio.Progress.Controller.ErrorNotification;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace SonarLint.VisualStudio.Progress.UnitTests
{
    /// <summary>
    /// Tests for <see cref="ErrorNotificationMananger"/>
    /// </summary>
    [TestClass]
    public class ErrorNotificationManagerTests
    {
        private ConfigurableServiceProvider serviceProvider;
        private ErrorNotificationManager testSubject;

        #region Test plumbing
        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestInitialize()
        {
            this.serviceProvider = new ConfigurableServiceProvider();
            this.testSubject = new ErrorNotificationManager();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            this.serviceProvider = null;
            this.testSubject = null;
        }
        #endregion

        #region Tests
        [TestMethod]
        [Description("Tests adding and removing notifiers")]
        public void ErrorNotificationMananger_EndToEnd()
        {
            ConfigurableErrorNotifier testNotifier = new ConfigurableErrorNotifier();

            // Should not throw
            this.Notify();

            // Add notifier
            this.testSubject.AddNotifier(testNotifier);
            this.Notify();
            testNotifier.AssertExcepections(1);

            // Cleanup
            this.testSubject.RemoveNotifier(testNotifier);

            // Add same notifier multiple times (no op)
            this.testSubject.AddNotifier(testNotifier);
            testNotifier.Reset();
            this.Notify();
            testNotifier.AssertExcepections(1);

            // Remove single instance
            this.testSubject.RemoveNotifier(testNotifier);
            testNotifier.Reset();
            this.Notify();
            testNotifier.AssertExcepections(0);

            // Remove non existing instance
            this.testSubject.RemoveNotifier(testNotifier);
            testNotifier.Reset();
            this.Notify();
            testNotifier.AssertExcepections(0);
        }
        #endregion

        #region Helpers
        private void Notify()
        {
            ((IProgressErrorNotifier)this.testSubject).Notify(new Exception(this.TestContext.TestName));
        }
        #endregion
    }
}
