/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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

using Newtonsoft.Json;
using SonarLint.VisualStudio.SLCore.Listener.Logger;

namespace SonarLint.VisualStudio.SLCore.Listeners.UnitTests.Logging
{
    public class LoggerSerializationTests
    {
        [TestMethod]
        [DataRow(0, LogLevel.ERROR)]
        [DataRow(1, LogLevel.WARN)]
        [DataRow(2, LogLevel.INFO)]
        [DataRow(3, LogLevel.DEBUG)]
        [DataRow(4, LogLevel.TRACE)]
        public void DeSerializeLogParams_IntegerEnums(int level, LogLevel expectedLevel)
        {
            var jsonString = $"{{\"message\":\"Some Message\",\"level\":{level}}}";

            var result = JsonConvert.DeserializeObject<LogParams>(jsonString);

            result.message.Should().Be("Some Message");
            result.level.Should().Be(expectedLevel);
        }

        [TestMethod]
        [DataRow("ERROR", LogLevel.ERROR)]
        [DataRow("WARN", LogLevel.WARN)]
        [DataRow("INFO", LogLevel.INFO)]
        [DataRow("DEBUG", LogLevel.DEBUG)]
        [DataRow("TRACE", LogLevel.TRACE)]
        public void DeSerializeLogParams_StringEnums(string level, LogLevel expectedLevel)
        {
            var jsonString = $"{{\"message\":\"Some Message\",\"level\":\"{level}\"}}";

            var result = JsonConvert.DeserializeObject<LogParams>(jsonString);

            result.message.Should().Be("Some Message");
            result.level.Should().Be(expectedLevel);
        }
    }
}
