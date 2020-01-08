/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
using FluentAssertions;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Progress.Controller.ErrorNotification;

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

        #endregion Test plumbing

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
            // Arrange
            bool logWholeMessage = true;
            Exception ex = this.Setup(logWholeMessage);

            // Act
            ((IProgressErrorNotifier)this.testSubject).Notify(ex);

            // Assert
            this.uiSHell.AssertMessageBoxShown();
        }

        [TestMethod]
        [Description("Verifies notifying of a full exception using a message box")]
        public void VsMessageBoxNotifier_FullException()
        {
            // Arrange
            bool logWholeMessage = true;
            Exception ex = this.Setup(logWholeMessage);

            // Act
            ((IProgressErrorNotifier)this.testSubject).Notify(ex);

            // Assert
            this.uiSHell.AssertMessageBoxShown();
        }

        #endregion Tests

        #region Helpers

        private Exception Setup(bool logWholeMessage)
        {
            string format = this.TestContext.TestName + "{0}";
            string title = this.TestContext.TestName;
            this.testSubject = new VsMessageBoxNotifier(this.serviceProvider, title, format, logWholeMessage);
            Exception ex = this.GenerateException();
            this.uiSHell.ShowMessageBoxAction = (actualTitle, actualMessage) =>
            {
                actualTitle.Should().Be(title, "Unexpected message box title");
                MessageVerificationHelper.VerifyNotificationMessage(actualMessage, format, ex, logWholeMessage);
            };
            return ex;
        }

        private Exception GenerateException()
        {
            return new Exception(this.TestContext.TestName, new Exception(Environment.TickCount.ToString()));
        }

        #endregion Helpers
    }
}