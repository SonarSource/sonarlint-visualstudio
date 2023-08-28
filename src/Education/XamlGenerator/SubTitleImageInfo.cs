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
            { RuleIssueSeverity.Critical, new SubTitleImageInfo("criticalDrawingImage", "Critical" )},
            { RuleIssueSeverity.Blocker, new SubTitleImageInfo("blockerDrawingImage", "Blocker" )},
            { RuleIssueSeverity.Major, new SubTitleImageInfo("majorDrawingImage", "Major" )},
            { RuleIssueSeverity.Minor, new SubTitleImageInfo("minorDrawingImage", "Minor" )},
            { RuleIssueSeverity.Info, new SubTitleImageInfo("infoDrawingImage", "Info" )},
            { RuleIssueSeverity.Unknown, new SubTitleImageInfo("infoDrawingImage", "Unknown" )}
        };

        public static readonly IReadOnlyDictionary<RuleIssueType, SubTitleImageInfo> IssueTypeImages = new Dictionary<RuleIssueType, SubTitleImageInfo>()
        {
            { RuleIssueType.Vulnerability, new SubTitleImageInfo("vulnerabilityDrawingImage", "Vulnerability" )},
            { RuleIssueType.CodeSmell, new SubTitleImageInfo("codeSmellDrawingImage", "Code Smell" )},
            { RuleIssueType.Bug, new SubTitleImageInfo("bugDrawingImage", "Bug" )},
            { RuleIssueType.Hotspot, new SubTitleImageInfo("hotspotDrawingImage", "Hotspot" )},
            { RuleIssueType.Unknown, new SubTitleImageInfo("codeSmellDrawingImage", "Unknown" )}
        };

        public static readonly IReadOnlyDictionary<SoftwareQualitySeverity, SubTitleImageInfo>
            SoftwareQualitySeveritiesImages = new Dictionary<SoftwareQualitySeverity, SubTitleImageInfo>()
            {
                // note: display text for these icons is dynamic and is set in XamlGeneratorHelper
                { SoftwareQualitySeverity.High , new SubTitleImageInfo("HighSoftwareQualitySeverity", string.Empty)},
                { SoftwareQualitySeverity.Medium , new SubTitleImageInfo("MediumSoftwareQualitySeverity", string.Empty)},
                { SoftwareQualitySeverity.Low , new SubTitleImageInfo("LowSoftwareQualitySeverity", string.Empty)},
            };

        private SubTitleImageInfo(string imageResourceName, string displayText)
        {
            ImageResourceName = imageResourceName;
            DisplayText = displayText;
        }

        /// <summary>
        /// Resource name to use in XAML to refer to the embedded image
        /// </summary>
        /// <remarks>The images are embedded in resource dictionaries and merged into tool window control.
        /// This is the name of the resource in the merged resource dictionary.</remarks>
        public string ImageResourceName { get; }

        /// <summary>
        /// Text to display for the image
        /// </summary>
        public string DisplayText { get; }
    }
}
