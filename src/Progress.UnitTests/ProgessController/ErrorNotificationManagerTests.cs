/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Progress.Controller.ErrorNotification;

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

        #endregion Test plumbing

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
            testNotifier.Exceptions.Should().HaveCount(1);

            // Cleanup
            this.testSubject.RemoveNotifier(testNotifier);

            // Add same notifier multiple times (no op)
            this.testSubject.AddNotifier(testNotifier);
            testNotifier.Reset();
            this.Notify();
            testNotifier.Exceptions.Should().HaveCount(1);

            // Remove single instance
            this.testSubject.RemoveNotifier(testNotifier);
            testNotifier.Reset();
            this.Notify();
            testNotifier.Exceptions.Should().BeEmpty();

            // Remove non existing instance
            this.testSubject.RemoveNotifier(testNotifier);
            testNotifier.Reset();
            this.Notify();
            testNotifier.Exceptions.Should().BeEmpty();
        }

        #endregion Tests

        #region Helpers

        private void Notify()
        {
            ((IProgressErrorNotifier)this.testSubject).Notify(new Exception(this.TestContext.TestName));
        }

        #endregion Helpers
    }
}