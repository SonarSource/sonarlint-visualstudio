/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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

        #endregion Test plumbing

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
            // Arrange
            bool logWholeMessage = true;
            Exception ex = this.Setup(logWholeMessage);

            // Act
            ((IProgressErrorNotifier)this.testSubject).Notify(ex);

            // Assert
            this.activityLog.IsEntryLogged.Should().BeTrue();
        }

        [TestMethod]
        [Description("Verifies logging of a full exception in activity log")]
        public void VsActivityLogNotifier_FullException()
        {
            // Arrange
            bool logWholeMessage = true;
            Exception ex = this.Setup(logWholeMessage);

            // Act
            ((IProgressErrorNotifier)this.testSubject).Notify(ex);

            // Assert
            this.activityLog.IsEntryLogged.Should().BeTrue();
        }

        #endregion Tests

        #region Helpers

        private Exception Setup(bool logWholeMessage)
        {
            string format = this.TestContext.TestName + "{0}";
            string source = this.TestContext.TestName;
            this.testSubject = new VsActivityLogNotifier(this.serviceProvider, source, format, logWholeMessage);
            Exception ex = this.GenerateException();
            this.activityLog.LogEntryAction = (entryType, actualSource, actualMessage) =>
            {
                entryType.Should().Be((uint)__ACTIVITYLOG_ENTRYTYPE.ALE_ERROR, "Unexpected entry type");
                actualSource.Should().Be(source, "Unexpected entry source");
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