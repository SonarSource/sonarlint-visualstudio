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
using System.Collections.ObjectModel;

namespace SonarQube.Client.Models
{
    public class SonarQubeRule
    {
        /// <summary>
        /// Singleton to prevent creating unnecessary objects
        /// </summary>
        private static readonly IReadOnlyDictionary<string, string> Empty = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());

        public SonarQubeRule(string key, string repositoryKey, bool isActive, SonarQubeIssueSeverity severity, IDictionary<string, string> parameters, SonarQubeIssueType issueType, string description, IReadOnlyList<SonarQubeDescriptionSection> descriptionSections, IReadOnlyList<string> educationPrinciples, string name, IReadOnlyList<string> tags, string htmlNote)

        {
            Key = key;
            RepositoryKey = repositoryKey;
            Description = description;
            IsActive = isActive;
            Severity = severity;
            IssueType = issueType;
            DescriptionSections = descriptionSections;
            EducationPrinciples = educationPrinciples;
            Name = name;
            Tags = tags;
            HtmlNote = htmlNote;

            if (parameters == null || parameters.Count == 0)
            {
                Parameters = Empty;
            }
            else
            {
                Parameters = new ReadOnlyDictionary<string, string>(parameters);
            }
        }

        public string Key { get; }

        public string RepositoryKey { get; }

        public string Description { get; set; }

        public bool IsActive { get; }

        public string Name { get; }

        public IReadOnlyList<string> Tags { get; }

        public SonarQubeIssueSeverity Severity { get; }

        /// <summary>
        /// When the rule is active, contains the parameters that are set in the corresponding quality profile.
        /// This is empty dictionary if the rule is inactive, or does not have parameters.
        /// </summary>
        public IReadOnlyDictionary<string, string> Parameters { get; }

        public SonarQubeIssueType IssueType { get; }

        public IReadOnlyList<SonarQubeDescriptionSection> DescriptionSections { get; }

        public IReadOnlyList<string> EducationPrinciples { get; }

        public string HtmlNote { get; }
    }
}
