﻿/*
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

using System;
using System.Collections.Generic;
using System.Linq;
using SonarQube.Client.Api.Common;
using SonarQube.Client.Models;

namespace SonarQube.Client.Helpers
{
    internal static class CleanCodeTaxonomyHelpers
    {
        internal static SonarQubeCleanCodeAttribute? ToSonarQubeCleanCodeAttribute(string value)
        {
            if (value == null)
            {
                return null;
            }

            return (SonarQubeCleanCodeAttribute)Enum.Parse(typeof(SonarQubeCleanCodeAttribute), value, true);
        }
        
        internal static Dictionary<SonarQubeSoftwareQuality, SonarQubeSoftwareQualitySeverity> ToDefaultImpacts(ServerImpact[] impacts)
        {
            return impacts?
                .ToDictionary(i => (SonarQubeSoftwareQuality)Enum.Parse(typeof(SonarQubeSoftwareQuality), i.SoftwareQuality, true),
                        i => (SonarQubeSoftwareQualitySeverity)Enum.Parse(typeof(SonarQubeSoftwareQualitySeverity), i.Severity, true));
        }
    }
}
