/*
 * SonarQube Client
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

using System.Collections.Generic;
using SonarQube.Client.Logging;

namespace SonarQube.Client.Tests
{
    public class TestLogger : ILogger
    {
        public List<string> DebugMessages { get; } = new List<string>();
        public List<string> ErrorMessages { get; } = new List<string>();
        public List<string> InfoMessages { get; } = new List<string>();
        public List<string> WarningMessages { get; } = new List<string>();

        public void Debug(string message)
        {
            DebugMessages.Add(message);
        }

        public void Error(string message)
        {
            ErrorMessages.Add(message);
        }

        public void Info(string message)
        {
            InfoMessages.Add(message);
        }

        public void Warning(string message)
        {
            WarningMessages.Add(message);
        }
    }
}
