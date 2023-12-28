/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using SonarLint.VisualStudio.SLCore.Listener;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Listener
{
    [TestClass]
    public class LoggerListenerTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<LoggerListener, ISLCoreListener>(MefTestHelpers.CreateExport<ILogger>());
        }

        [TestMethod]
        public void Mef_CheckIsSingleton()
        {
            MefTestHelpers.CheckIsSingletonMefComponent<LoggerListener>();
        }

        [TestMethod]
        [DataRow(LogLevel.ERROR, true)]
        [DataRow(LogLevel.WARN, true)]
        [DataRow(LogLevel.INFO, false)]
        [DataRow(LogLevel.TRACE, false)]
        [DataRow(LogLevel.DEBUG, false)]
        public void Log_LogsOnlyErrorAndWarning(LogLevel logLevel, bool logs)
        {
            var param = new LogParams { level = logLevel, message = "some Message" };

            var logger = new TestLogger();

            var testSubject = new LoggerListener(logger);

            testSubject.Log(param);

            if (logs)
            {
                logger.AssertOutputStringExists("[SLCORE] some Message");
            }
            else
            {
                logger.AssertNoOutputMessages();
            }
        }
    }
}
