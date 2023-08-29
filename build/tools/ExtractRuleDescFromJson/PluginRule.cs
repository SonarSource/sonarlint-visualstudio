/*
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

using SonarLint.VisualStudio.Core.Analysis;

namespace ExtractRuleDescFromJson;

/// <summary>
/// Data-class: rule properties, as defined by the plugin API
/// </summary>
public class PluginRule
{
    public string? Key { get; set; }

    public string? Name { get; set; }

    public string? DefaultSeverity { get; set; }

    public string? Type { get; set; }

    public string? Description { get; set; }

    public bool? IsActiveByDefault { get; set; }

    public string? Language { get; set; }

    public string[]? Tags { get; set; }

    public DescriptionSection[]? DescriptionSections { get; set; }

    public string[]? EducationPrinciples { get; set; }

    public CleanCodeAttribute? CleanCodeAttribute { get; set; }

    public Dictionary<SoftwareQuality, SoftwareQualitySeverity>? DefaultImpacts { get; set; }
}

public class DescriptionSection
{
    public DescriptionSection(string key, string htmlContent, DescriptionSectionContext context = null)
    {
        Key = key;
        HtmlContent = htmlContent;
        Context = context;
    }

    public string Key { get; }
    public string HtmlContent { get; }

    public DescriptionSectionContext? Context { get; }
}

public class DescriptionSectionContext
{
    public DescriptionSectionContext(string key, string displayName)
    {
        Key = key;
        DisplayName = displayName;
    }

    public string Key { get; }
    public string DisplayName { get; }
}
