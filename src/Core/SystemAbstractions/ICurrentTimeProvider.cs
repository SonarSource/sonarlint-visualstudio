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

namespace SonarLint.VisualStudio.Core.SystemAbstractions
{
    /// <summary>
    ///  Abstraction over DateTimeOffset.Now for testing
    /// </summary>
    public interface ICurrentTimeProvider
    {
        /// <summary>
        /// Returns the current date and time
        /// </summary>
        DateTimeOffset Now { get; }

        /// <summary>
        /// Returns the local time zone
        /// </summary>
        TimeZoneInfo LocalTimeZone { get; }
    }

    public sealed class DefaultCurrentTimeProvider : ICurrentTimeProvider
    {
        public static ICurrentTimeProvider Instance { get; } = new DefaultCurrentTimeProvider();

        private DefaultCurrentTimeProvider()
        {
            // Can't be publicly constructed
        }

        DateTimeOffset ICurrentTimeProvider.Now => DateTimeOffset.Now;

        TimeZoneInfo ICurrentTimeProvider.LocalTimeZone => TimeZoneInfo.Local;
    }
}
