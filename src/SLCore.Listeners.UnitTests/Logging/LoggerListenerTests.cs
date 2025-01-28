/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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

using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Listener.Logger;

namespace SonarLint.VisualStudio.SLCore.Listeners.UnitTests.Logging
{
    [TestClass]
    public class LoggerListenerTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported() =>
            MefTestHelpers.CheckTypeCanBeImported<LoggerListener, ISLCoreListener>(MefTestHelpers.CreateExport<ILogger>());

        [TestMethod]
        public void Mef_CheckIsSingleton() =>
            MefTestHelpers.CheckIsSingletonMefComponent<LoggerListener>();

        [TestMethod]
        [DataRow(LogLevel.ERROR, false)]
        [DataRow(LogLevel.WARN, false)]
        [DataRow(LogLevel.INFO, true)]
        [DataRow(LogLevel.TRACE, true)]
        [DataRow(LogLevel.DEBUG, true)]
        public void Log_LogInfoTraceAndDebugAsVerbose(LogLevel logLevel, bool verboseLogs)
        {
            var param = new LogParams { level = logLevel, message = "some Message" };

            var logger = new TestLogger();

            var testSubject = new LoggerListener(logger);

            testSubject.Log(param);

            logger.AssertOutputStringExists(verboseLogs ? "[DEBUG] [SLCore] some Message" : "[SLCore] some Message");
        }

        [TestMethod]
        public void Ctor_AddsProperties()
        {
            var logger = Substitute.For<ILogger>();
            _ = new LoggerListener(logger);

            logger.Received(1).ForContext(SLCoreStrings.SLCoreName);
        }

        [TestMethod]
        public void Log_AddsArgumentProperties()
        {
            var customizedLogger = Substitute.For<ILogger>();
            var logger = Substitute.For<ILogger>();
            logger.ForContext(SLCoreStrings.SLCoreName).Returns(customizedLogger);
            var testSubject  = new LoggerListener(logger);

            testSubject.Log(new LogParams{configScopeId = "configScopeId1", loggerName = "loggerName1", threadName = "threadName1", message = "msg"});
            testSubject.Log(new LogParams{configScopeId = "configScopeId2", loggerName = "loggerName2", threadName = "threadName2", stackTrace = "stackTrace"});

            customizedLogger.Received(1).WriteLine(Arg.Is<MessageLevelContext>(ctx => ctx.VerboseContext.SequenceEqual(new []{"loggerName1", "configScopeId1", "threadName1"})), Arg.Any<string>(), Arg.Any<object[]>());
            customizedLogger.Received(1).LogVerbose(Arg.Is<MessageLevelContext>(ctx => ctx.VerboseContext.SequenceEqual(new []{"loggerName2", "configScopeId2", "threadName2"})), Arg.Any<string>(), Arg.Any<object[]>());
        }

        [TestMethod]
        public void Log_ProducesCorrectFullFormat()
        {
            var testLogger = new TestLogger(logVerbose:false);
            var testSubject = new LoggerListener(testLogger);

            testSubject.Log(new LogParams
            {
                loggerName = "loggerName",
                configScopeId = "configScopeId",
                threadName = "threadName",
                message = "message",
                stackTrace = """
                             stack
                             trace
                             """
            });

            testLogger.AssertOutputStrings("[SLCore] message");
        }

        [TestMethod]
        public void Log_ProducesCorrectFullVerboseFormat()
        {
            var testLogger = new TestLogger();
            var testSubject = new LoggerListener(testLogger);

            testSubject.Log(new LogParams
            {
                loggerName = "loggerName",
                configScopeId = "configScopeId",
                threadName = "threadName",
                message = "message",
                stackTrace = """
                             stack
                             trace
                             """
            });

            testLogger.AssertOutputStrings(
                "[SLCore] [loggerName > configScopeId > threadName] message",
                """
                [DEBUG] [SLCore] [loggerName > configScopeId > threadName] stack
                trace
                """);
        }

        [TestMethod]
        public void Log_NullablePropertiesMissingExceptMessage_ProducesCorrectMessage()
        {
            var testLogger = new TestLogger();
            var testSubject = new LoggerListener(testLogger);

            testSubject.Log(new LogParams
            {
                loggerName = "loggerName",
                configScopeId = null,
                threadName = "threadName",
                message = "message",
                stackTrace = null
            });

            testLogger.AssertOutputStrings("[SLCore] [loggerName > threadName] message");
        }

        [TestMethod]
        public void Log_NullablePropertiesMissingExceptStackTrace_ProducesCorrectMessage()
        {
            var testLogger = new TestLogger(logVerbose:true);
            var testSubject = new LoggerListener(testLogger);

            testSubject.Log(new LogParams
            {
                loggerName = "loggerName",
                configScopeId = null,
                threadName = "threadName",
                message = null,
                stackTrace = """
                             stack
                             trace
                             """
            });

            testLogger.AssertOutputStrings("""
                                           [DEBUG] [SLCore] [loggerName > threadName] stack
                                           trace
                                           """);
        }
    }
}
