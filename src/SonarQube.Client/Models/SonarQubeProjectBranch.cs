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

using System;

namespace SonarQube.Client.Models
{
    public class SonarQubeProjectBranch
    {
        public string Name { get; }
        public bool IsMain { get; }
        public DateTimeOffset LastAnalysisTimestamp { get; }
        public string Type { get; set; }

        public SonarQubeProjectBranch(string name, bool isMain, DateTimeOffset analysisDate, string type)
        {
            Name = name;
            IsMain = isMain;
            LastAnalysisTimestamp = analysisDate;
            Type = type;
        }
    }
}
