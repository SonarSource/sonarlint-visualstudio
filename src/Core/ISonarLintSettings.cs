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

namespace SonarLint.VisualStudio.Integration
{
    public enum DaemonLogLevel { Verbose, Info, Minimal };

    public interface ISonarLintSettings
    {
        /// <summary>
        /// True if support for analysing additional languages is enabled.
        /// </summary>
        /// <remarks>
        /// Note: this setting is no longer used or user-settable now that the JS analysis is NodeJS-based.
        /// Ticket #2307 covers removing the setting and cleaning up the non-NodeJS daemon.
        /// </remarks>
        bool IsActivateMoreEnabled { get; set; }
        DaemonLogLevel DaemonLogLevel { get; set; }
        string JreLocation { get; set; }
    }
}
