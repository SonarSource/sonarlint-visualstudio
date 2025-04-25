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

using System.ComponentModel.Composition;
using System.Text;
using SonarLint.VisualStudio.Core.CSharpVB;

namespace SonarLint.VisualStudio.Integration.CSharpVB;

public interface IGlobalConfigGenerator
{
    string Generate(IEnumerable<IRoslynRuleStatus> rules);
}

[Export(typeof(IGlobalConfigGenerator))]
[PartCreationPolicy(CreationPolicy.Shared)]
public class GlobalConfigGenerator : IGlobalConfigGenerator
{
    [ImportingConstructor]
    public GlobalConfigGenerator() { }

    public string Generate(IEnumerable<IRoslynRuleStatus> rules)
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
            var severityText = GetActionText(rule.GetSeverity());

            sb.AppendLine($"dotnet_diagnostic.{rule.Key}.severity = {severityText}");
        }

        return sb.ToString();
    }

    // See https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/configuration-options#severity-level
    private static string GetActionText(RuleAction ruleAction) =>
        ruleAction switch
        {
            RuleAction.None => "none",
            RuleAction.Info => "suggestion",
            RuleAction.Warning => "warning",
            RuleAction.Error => "error",
            RuleAction.Hidden => "silent",
            _ => throw new ArgumentOutOfRangeException($"{ruleAction} is not a supported RuleAction.")
        };
}
