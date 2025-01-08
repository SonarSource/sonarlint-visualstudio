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
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Protocol;
using Language = SonarLint.VisualStudio.SLCore.Common.Models.Language;

namespace SonarLint.VisualStudio.SLCore.Service.Rules.Models;

public record EffectiveRuleDetailsDto(
    string key,
    string name,
    Language language,
    [property: JsonConverter(typeof(EitherJsonConverter<StandardModeDetails, MQRModeDetails>))]
    Either<StandardModeDetails, MQRModeDetails> severityDetails,
    VulnerabilityProbability? vulnerabilityProbability,
    [property: JsonConverter(typeof(EitherJsonConverter<RuleMonolithicDescriptionDto, RuleSplitDescriptionDto>))]
    Either<RuleMonolithicDescriptionDto, RuleSplitDescriptionDto> description,
    [JsonProperty("params")] List<EffectiveRuleParamDto> parameters) : IRuleDetails;

public record StandardModeDetails(IssueSeverity severity, RuleType type);
public record MQRModeDetails(CleanCodeAttribute cleanCodeAttribute, List<ImpactDto> impacts);
