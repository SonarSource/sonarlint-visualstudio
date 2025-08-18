/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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
using Newtonsoft.Json.Converters;

namespace SonarLint.VisualStudio.SLCore.Common.Models;

public record DependencyRiskDto(
    Guid id,
    DependencyRiskType type,
    DependencyRiskSeverity severity,
    DependencyRiskStatus status,
    string packageName,
    string packageVersion,
    string? vulnerabilityId,
    string? cvssScore,
    List<DependencyRiskTransition> transitions);

public enum DependencyRiskSeverity
{
    INFO, LOW, MEDIUM, HIGH, BLOCKER
}

public enum DependencyRiskType
{
    VULNERABILITY, PROHIBITED_LICENSE
}

public enum DependencyRiskStatus
{
    FIXED, OPEN, CONFIRM, ACCEPT, SAFE
}

[JsonConverter(typeof(StringEnumConverter))]
public enum DependencyRiskTransition
{
    CONFIRM, REOPEN, SAFE, FIXED, ACCEPT
}
