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
using SonarLint.VisualStudio.Rules;

namespace SonarLint.VisualStudio.Education.XamlGenerator
{
    /// <summary>
    /// Data class - holds the image path and display text to use for sub-element items
    /// with icons i.e. severity and issue type
    /// </summary>
    public class SubTitleImageInfo
    {
        public static readonly IReadOnlyDictionary<RuleIssueSeverity, SubTitleImageInfo> SeverityImages = new Dictionary<RuleIssueSeverity, SubTitleImageInfo>()
        {
            { RuleIssueSeverity.Critical, new SubTitleImageInfo("/SonarLint.VisualStudio.Education;component/XamlGenerator/images/severity/critical.png", "Critical" )},
            { RuleIssueSeverity.Blocker, new SubTitleImageInfo("/SonarLint.VisualStudio.Education;component/XamlGenerator/images/severity/blocker.png", "Blocker" )},
            { RuleIssueSeverity.Major, new SubTitleImageInfo("/SonarLint.VisualStudio.Education;component/XamlGenerator/images/severity/major.png", "Major" )},
            { RuleIssueSeverity.Minor, new SubTitleImageInfo("/SonarLint.VisualStudio.Education;component/XamlGenerator/images/severity/minor.png", "Minor" )},
            { RuleIssueSeverity.Info, new SubTitleImageInfo("/SonarLint.VisualStudio.Education;component/XamlGenerator/images/severity/info.png", "Info" )}
        };

        public static readonly IReadOnlyDictionary<RuleIssueType, SubTitleImageInfo> IssueTypeImages = new Dictionary<RuleIssueType, SubTitleImageInfo>()
        {
            { RuleIssueType.Vulnerability, new SubTitleImageInfo("/SonarLint.VisualStudio.Education;component/XamlGenerator/images/type/vulnerability.png", "Vulnerability" )},
            { RuleIssueType.CodeSmell, new SubTitleImageInfo("/SonarLint.VisualStudio.Education;component/XamlGenerator/images/type/code_smell.png", "Code Smell" )},
            { RuleIssueType.Bug, new SubTitleImageInfo("/SonarLint.VisualStudio.Education;component/XamlGenerator/images/type/bug.png", "Bug" )},
            { RuleIssueType.Hotspot, new SubTitleImageInfo(null /* TODO - image for hotspot */, "Hotspot" )},
        };

        private SubTitleImageInfo(string imageResourceName, string displayText)
        {
            ImageResourcePath = imageResourceName;
            DisplayText = displayText;
        }

        /// <summary>
        /// Path to use in XAML to refer to the embedded image
        /// </summary>
        public string ImageResourcePath { get; }

        /// <summary>
        /// Text to display for the image
        /// </summary>
        public string DisplayText { get; }

    }
}
