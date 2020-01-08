/*
 * SonarQube Client
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

namespace SonarQube.Client.Models
{
    public enum SonarQubeIssueSeverity
    {
        Unknown = 0,
        Info = 1,
        Minor = 2,
        Major = 3,
        Critical = 4,
        Blocker = 5
    }

    internal static class SonarQubeIssueSeverityConverter
    {
        public static SonarQubeIssueSeverity Convert(string data)
        {
            SonarQubeIssueSeverity severity;
            if (!Enum.TryParse(data, true /* ignore case */, out severity))
            {
                return SonarQubeIssueSeverity.Unknown;
            }
            return severity;
        }
    }
}
