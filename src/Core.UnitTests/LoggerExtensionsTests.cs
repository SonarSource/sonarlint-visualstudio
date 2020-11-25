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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.Helpers;
using SonarLint.VisualStudio.Integration.UnitTests;

namespace SonarLint.VisualStudio.Core.UnitTests
{
    [TestClass]
    public class LoggerExtensionsTests
    {
        [TestMethod]
        public void LogDebug_IsLogging_SimpleMessage_IsLogged()
        {
            SetupAndExecute(shouldLog: true, logger =>
            {
                logger.LogDebug("a message");
                logger.AssertPartialOutputStringExists("a message");
            });
        }

        [TestMethod]
        public void LogDebug_IsLogging_FormattedMessage_IsCorrectlyFormattedAndLogged()
        {
            SetupAndExecute(shouldLog: true, logger =>
            {
                logger.LogDebug("aaa {0} {1}", "bbb", 123);
                logger.AssertPartialOutputStringExists("aaa bbb 123");
            });
        }

        [TestMethod]
        public void LogDebug_IsNotLogging_NoMessagesLogged()
        {
            SetupAndExecute(shouldLog: false, logger =>
            {
                logger.LogDebug("a message");
                logger.AssertNoOutputMessages();
            });
        }

        private static void SetupAndExecute(bool shouldLog, Action<TestLogger> testOp)
        {
            var logger = new TestLogger();
            var mockSettings = new Mock<IEnvironmentSettings>();
            mockSettings.Setup(x => x.LogDebugMessages()).Returns(shouldLog);

            try
            {
                ILoggerExtensions.Initialize(mockSettings.Object);

                // Act
                testOp(logger);
            }
            finally
            {
                // Re-initialize from whatever the real environment settings are
                ILoggerExtensions.Initialize(new EnvironmentSettings());
            }
        }
    }
}
