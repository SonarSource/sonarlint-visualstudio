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

// Note: we have multiple enum definitions for IssueSeverity and IssueType in SLVS
// They should be consolidated if possible.
// See https://github.com/SonarSource/sonarlint-visualstudio/issues/3617

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SonarLint.VisualStudio.Rules
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

    public interface IContext
    {
        string Key { get; }
        string DisplayName { get; }
    }

    public class Context : IContext
    {
        public Context(string key, string displayName)
        {
            Key = key;
            DisplayName = displayName;
        }

        public string Key { get; }
        public string DisplayName { get; }
    }

    public interface IDescriptionSection
    {
        string Key { get; }
        string HtmlContent { get; }
        IContext Context { get; }
    }

    public class DescriptionSection : IDescriptionSection
    {
        public DescriptionSection(string key, string htmlContent, IContext context = null)
        {
            Key = key;
            HtmlContent = htmlContent;
            Context = context;
        }

        public string Key { get; }
        public string HtmlContent { get; }

        /// <summary>
        /// Different contexes for Description Sections
        /// </summary>
        /// <remarks>
        /// These are subtabs for any given tab such as different languages
        /// The field is optional
        /// </remarks>
        public IContext Context { get; }
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

        [JsonProperty("DefaultSeverity")]
        RuleIssueSeverity Severity { get; }

        RuleIssueType IssueType { get; }

        bool IsActiveByDefault { get; }

        string LanguageKey { get; }

        /// <summary>
        /// The HTML description, tweaked so it can be parsed as XML
        /// </summary>
        string Description { get; }

        /// <summary>
        /// List of tags. Can be empty. Will not be null.
        /// </summary>
        IReadOnlyList<string> Tags { get; }

        /// <summary>
        /// Tabs for new educational format. Can be empty. Will not be null.
        /// </summary>
        IReadOnlyList<IDescriptionSection> DescriptionSections { get; }

        /// <summary>
        /// Education principles for the new educational format. Can be empty. Will not be null.
        /// </summary>
        IReadOnlyList<string> EducationPrinciples { get; }

        string HtmlNote { get; }

        CleanCodeAttribute? CleanCodeAttribute { get; }

        Dictionary<SoftwareQuality, SoftwareQualitySeverity> DefaultImpacts { get; }

        IRuleInfo WithServerOverride(RuleIssueSeverity newSeverity, string newHtmlNote);
    }

    public class RuleInfo : IRuleInfo
    {
        public RuleInfo(string languageKey, string fullRuleKey, string description, string name,
            RuleIssueSeverity severity, RuleIssueType issueType, bool isActiveByDefault,
            IReadOnlyList<string> tags, IReadOnlyList<IDescriptionSection> descriptionSections, IReadOnlyList<string> educationPrinciples, string htmlNote,
            CleanCodeAttribute? cleanCodeAttribute, Dictionary<SoftwareQuality, SoftwareQualitySeverity> defaultImpacts)
        {
            LanguageKey = languageKey;
            FullRuleKey = fullRuleKey;
            Description = description;
            Name = name;
            Severity = severity;
            IssueType = issueType;
            IsActiveByDefault = isActiveByDefault;
            Tags = tags ?? Array.Empty<string>();
            DescriptionSections = descriptionSections ?? Array.Empty<IDescriptionSection>();
            EducationPrinciples = educationPrinciples ?? Array.Empty<string>();
            HtmlNote = htmlNote;
            CleanCodeAttribute = cleanCodeAttribute;
            DefaultImpacts = defaultImpacts ?? new Dictionary<SoftwareQuality, SoftwareQualitySeverity>();
        }

        public string FullRuleKey { get; private set; }

        public string Name { get; private set; }

        public RuleIssueSeverity Severity { get; set; }

        public RuleIssueType IssueType { get; private set; }

        public bool IsActiveByDefault { get; private set; }

        public string LanguageKey { get; private set; }

        public string Description { get; private set; }

        public IReadOnlyList<string> Tags { get; private set; }

        public IReadOnlyList<IDescriptionSection> DescriptionSections { get; }

        public IReadOnlyList<string> EducationPrinciples { get; }

        public string HtmlNote { get; }

        public CleanCodeAttribute? CleanCodeAttribute { get; }

        public Dictionary<SoftwareQuality, SoftwareQualitySeverity> DefaultImpacts { get; }

        public IRuleInfo WithServerOverride(RuleIssueSeverity newSeverity, string newHtmlNote)
        {
            return new RuleInfo(LanguageKey, FullRuleKey, Description, Name, newSeverity, IssueType, IsActiveByDefault, Tags, DescriptionSections, EducationPrinciples, newHtmlNote, CleanCodeAttribute, DefaultImpacts);
        }
    }
}
