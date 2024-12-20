﻿/*
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

using System.ComponentModel.Composition;

namespace SonarLint.VisualStudio.Core
{
    public interface ILogger
    {
        /// <summary>
        /// Logs a message and appends a new line.
        /// </summary>
        void WriteLine(string message);

        void WriteLine(string messageFormat, params object[] args);

        /// <summary>
        /// Logs a message and appends a new line if logging is set to verbose. Otherwise does nothing.
        /// </summary>
        void LogVerbose(string messageFormat, params object[] args);

        ILogger ForContext(params string[] context);

        ILogger ForVerboseContext(params string[] context);
    }

    public interface ILogWriter
    {
        void WriteLine(string message);
    }

    public interface ILogVerbosityIndicator
    {
        bool IsVerboseEnabled { get; }
        bool IsThreadIdEnabled { get; }
    }
}
