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

using System.Text;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.CSharpVB;
using SonarQube.Client.Models;
using static SonarQube.Client.Models.SonarQubeSoftwareQualitySeverity;

namespace SonarLint.VisualStudio.ConnectedMode.Binding;

public interface IGlobalConfigGenerator
{
    string Generate(IEnumerable<SonarQubeRule> rules);
}

public class GlobalConfigGenerator : IGlobalConfigGenerator
{
    private readonly IEnvironmentSettings environmentSettings;

    private static readonly string inactiveRuleActionText = GetActionText(RuleAction.None);

    public GlobalConfigGenerator() : this(new EnvironmentSettings()) { }

    public GlobalConfigGenerator(IEnvironmentSettings environmentSettings)
    {
        this.environmentSettings = environmentSettings;
    }

    public string Generate(IEnumerable<SonarQubeRule> rules)
    {
        if (rules == null)
        {
            throw new ArgumentNullException(nameof(rules));
        }

        var sb = new StringBuilder();

        sb.AppendLine("is_global=true");
        sb.AppendLine("global_level=1999999999");

        var sortedRules = rules.OrderBy(r => r.Key);

        foreach (var rule in sortedRules)
        {
            var severityText = rule.IsActive ? GetActionText(GetVsSeverity(rule)) : inactiveRuleActionText;

            sb.AppendLine($"dotnet_diagnostic.{rule.Key}.severity = {severityText}");
        }

        return sb.ToString();
    }

    internal  /* for testing */ RuleAction GetVsSeverity(SonarQubeRule rule) =>
        GetVsSeverity(rule.SoftwareQualitySeverities?.Values) ?? GetVsSeverity(rule.Severity);

    internal  /* for testing */ RuleAction? GetVsSeverity(ICollection<SonarQubeSoftwareQualitySeverity> severities) =>
        severities is not { Count: > 0 }
            ? null
            : GetVsSeverity(severities.Max());

    private RuleAction GetVsSeverity(SonarQubeSoftwareQualitySeverity severity) =>
        severity switch
        {
            Blocker or High => environmentSettings.TreatBlockerSeverityAsError() ? RuleAction.Error : RuleAction.Warning,
            Medium => RuleAction.Warning,
            Low or Info => RuleAction.Info,
            _ => throw new NotSupportedException($"Unsupported SonarQube issue severity: {severity}")
        };

    internal /* for testing */ RuleAction GetVsSeverity(SonarQubeIssueSeverity sqSeverity) =>
        sqSeverity switch
        {
            SonarQubeIssueSeverity.Info or SonarQubeIssueSeverity.Minor => RuleAction.Info,
            SonarQubeIssueSeverity.Major or SonarQubeIssueSeverity.Critical => RuleAction.Warning,
            SonarQubeIssueSeverity.Blocker => environmentSettings.TreatBlockerSeverityAsError() ? RuleAction.Error : RuleAction.Warning,
            _ => throw new NotSupportedException($"Unsupported SonarQube issue severity: {sqSeverity}")
        };

    // See https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/configuration-options#severity-level
    internal /* for testing */ static string GetActionText(RuleAction ruleAction) =>
        ruleAction switch
        {
            RuleAction.None => "none",
            RuleAction.Info => "suggestion",
            RuleAction.Warning => "warning",
            RuleAction.Error => "error",
            RuleAction.Hidden => "silent",
            _ => throw new NotSupportedException($"{ruleAction} is not a supported RuleAction.")
        };
}
