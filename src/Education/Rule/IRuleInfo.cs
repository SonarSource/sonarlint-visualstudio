/*
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

// Note: we have multiple enum definitions for IssueSeverity and IssueType in SLVS
// They should be consolidated if possible.
// See https://github.com/SonarSource/sonarlint-visualstudio/issues/3617

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.SLCore.Service.Rules.Models;

namespace SonarLint.VisualStudio.Education.Rule
{
    public enum RuleIssueSeverity
    {
        Blocker,
        Critical,
        Major,
        Minor,
        Info,
        Unknown
    }

    public enum RuleIssueType
    {
        CodeSmell,      // SonarQube serialization = CODE_SMELL
        Bug,
        Vulnerability,
        Hotspot,        // SonarQube serialization = SECURITY_HOTSPOT
        Unknown
    }
    
    /// <summary>
    /// Help data about a single rule, extracted using the Java plugin API
    /// </summary>
    public interface IRuleInfo
    {
        /// <summary>
        /// Unique identifier for the rule, including the repo key e.g. cpp:S123
        /// </summary>
        string FullRuleKey { get; }

        string Name { get; }
        
        RuleIssueSeverity Severity { get; }

        RuleIssueType IssueType { get; }

        /// <summary>
        /// The HTML description, tweaked so it can be parsed as XML
        /// </summary>
        string Description { get; }
        
        RuleSplitDescriptionDto RichRuleDescriptionDto { get; }

        CleanCodeAttribute? CleanCodeAttribute { get; }

        Dictionary<SoftwareQuality, SoftwareQualitySeverity> DefaultImpacts { get; }
    }

    public class RuleInfo : IRuleInfo
    {
        public RuleInfo(string fullRuleKey, string description, string name,
            RuleIssueSeverity severity, RuleIssueType issueType, RuleSplitDescriptionDto richRuleDescriptionDto,
            CleanCodeAttribute? cleanCodeAttribute, Dictionary<SoftwareQuality, SoftwareQualitySeverity> defaultImpacts)
        {
            FullRuleKey = fullRuleKey;
            Description = description;
            Name = name;
            Severity = severity;
            IssueType = issueType;
            RichRuleDescriptionDto = richRuleDescriptionDto;
            CleanCodeAttribute = cleanCodeAttribute;
            DefaultImpacts = defaultImpacts ?? new Dictionary<SoftwareQuality, SoftwareQualitySeverity>();
        }

        public string FullRuleKey { get; private set; }

        public string Name { get; private set; }

        public RuleIssueSeverity Severity { get; set; }

        public RuleIssueType IssueType { get; private set; }

        public string Description { get; private set; }

        public RuleSplitDescriptionDto RichRuleDescriptionDto { get; set; }

        public CleanCodeAttribute? CleanCodeAttribute { get; }

        public Dictionary<SoftwareQuality, SoftwareQualitySeverity> DefaultImpacts { get; }
    }
}
