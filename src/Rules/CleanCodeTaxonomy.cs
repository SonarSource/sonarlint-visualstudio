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

using System.Collections.Generic;

namespace SonarLint.VisualStudio.Rules
{
    /// <summary>
    /// Clean Code Category describes the category the issue falls into
    /// </summary>
    public enum CleanCodeCategory
    {
        Consistency,
        Intentionality,
        Adaptability,
        Responsibility
    }

    /// <summary>
    /// Clean Code Attribute describes a particular aspect of <see cref="CleanCodeCategory"/>
    /// </summary>
    public enum CleanCodeAttribute
    {
        // Consistency
        Conventional,
        Formatted,
        Identifiable,
        // Intentionality
        Clear,
        Complete,
        Efficient,
        Logical,
        // Adaptability
        Distinct,
        Focused,
        Modular,
        Tested,
        // Responsibility
        Lawful,
        Respectful,
        Trustworthy
    }

    /// <summary>
    /// Mapping from <see cref="CleanCodeAttribute"/> to corresponding <see cref="CleanCodeCategory"/>
    /// </summary>
    public static class CleanCodeAttributeToCategoryMapping
    {
        public static IReadOnlyDictionary<CleanCodeAttribute, CleanCodeCategory> Map { get; } =
            new Dictionary<CleanCodeAttribute, CleanCodeCategory>
            {
                { CleanCodeAttribute.Conventional, CleanCodeCategory.Consistency },
                { CleanCodeAttribute.Formatted, CleanCodeCategory.Consistency },
                { CleanCodeAttribute.Identifiable, CleanCodeCategory.Consistency },
                { CleanCodeAttribute.Clear, CleanCodeCategory.Intentionality },
                { CleanCodeAttribute.Complete, CleanCodeCategory.Intentionality },
                { CleanCodeAttribute.Efficient, CleanCodeCategory.Intentionality },
                { CleanCodeAttribute.Logical, CleanCodeCategory.Intentionality },
                { CleanCodeAttribute.Distinct, CleanCodeCategory.Adaptability },
                { CleanCodeAttribute.Focused, CleanCodeCategory.Adaptability },
                { CleanCodeAttribute.Modular, CleanCodeCategory.Adaptability },
                { CleanCodeAttribute.Tested, CleanCodeCategory.Adaptability },
                { CleanCodeAttribute.Lawful, CleanCodeCategory.Responsibility },
                { CleanCodeAttribute.Respectful, CleanCodeCategory.Responsibility },
                { CleanCodeAttribute.Trustworthy, CleanCodeCategory.Responsibility },
            };
    }

    /// <summary>
    /// Software Quality describes the aspect of the software negatively affected by the issue
    /// </summary>
    public enum SoftwareQuality
    {
        Maintainability,
        Reliability,
        Security
    }

    /// <summary>
    /// Software Quality Severity describes how severe is the impact of the issue on the <see cref="SoftwareQuality"/>
    /// </summary>
    public enum SoftwareQualitySeverity
    {
        High,
        Medium,
        Low
    }
}
