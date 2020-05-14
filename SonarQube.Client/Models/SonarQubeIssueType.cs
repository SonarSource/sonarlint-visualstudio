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

namespace SonarQube.Client.Models
{
    public enum SonarQubeIssueType
    {
        Unknown = 0,
        CodeSmell = 1,
        Bug = 2,
        Vulnerability = 3,
        SecurityHotspot = 4
    }

    internal static class SonarQubeIssueTypeConverter
    {
        public static SonarQubeIssueType Convert(string data)
        {
            switch (data?.ToUpperInvariant())
            {
                case "CODE_SMELL":
                    return SonarQubeIssueType.CodeSmell;
                case "BUG":
                    return SonarQubeIssueType.Bug;
                case "SECURITY_HOTSPOT":
                    return SonarQubeIssueType.SecurityHotspot;
                case "VULNERABILITY":
                    return SonarQubeIssueType.Vulnerability;
                default:
                    return SonarQubeIssueType.Unknown;
            }
        }
    }
}
