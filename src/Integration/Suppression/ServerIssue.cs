/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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

namespace SonarLint.VisualStudio.Integration.Suppression
{
    /// <summary>
    /// TODO: placeholder class: data contract for server-side issues
    /// </summary>
    /// <remarks>Maps to the fields returned by SonarQube web service GET /batch/issues</remarks>
    public class ServerIssue
    {
//        String Key { get; set; }
        public string ModuleKey { get; set; }
        public string Path { get; set; }
        public string RuleRespository { get; set; }
        public string RuleKey { get; set; }
        public int Line { get; set; }
        public string Message { get; set; }
        //        String Severity { get; set; }
        //        bool ManualSeverity { get; set; }
        public string Resolution { get; set; }
        public string Status { get; set; }
        public string Checksum { get; set; }
        //        String AssigneeLogin { get; set; }
        //        long CreationDate { get; set; }
    }
}
