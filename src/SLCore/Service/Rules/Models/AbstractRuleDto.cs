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

using System.Collections.Generic;
using SonarLint.VisualStudio.SLCore.Common.Models;

namespace SonarLint.VisualStudio.SLCore.Service.Rules.Models
{
    public abstract class AbstractRuleDto
    {
        protected AbstractRuleDto(string key,
            string name,
            IssueSeverity severity,
            RuleType type,
            CleanCodeAttribute? cleanCodeAttribute,
            CleanCodeAttributeCategory? cleanCodeAttributeCategory,
            List<ImpactDto> defaultImpacts,
            Language language,
            VulnerabilityProbability? vulnerabilityProbability)
        {
            this.key = key;
            this.name = name;
            this.severity = severity;
            this.type = type;
            this.cleanCodeAttribute = cleanCodeAttribute;
            this.cleanCodeAttributeCategory = cleanCodeAttributeCategory;
            this.defaultImpacts = defaultImpacts;
            this.language = language;
            this.vulnerabilityProbability = vulnerabilityProbability;
        }

        public string key { get; }
        public string name { get; }
        public IssueSeverity severity { get; }
        public RuleType type { get; }
        public CleanCodeAttribute? cleanCodeAttribute { get; }
        public CleanCodeAttributeCategory? cleanCodeAttributeCategory { get; }
        public List<ImpactDto> defaultImpacts { get; }
        public Language language { get; }
        public VulnerabilityProbability? vulnerabilityProbability { get; }
    }
}
