/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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

using System.IO;
using System.Linq;
using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using SonarLint.VisualStudio.Integration.Rules;
using SonarQube.Client.Messages;

namespace SonarLint.VisualStudio.Integration.Helpers
{
    internal static class RulesHelper
    {
        public static RuleSet ToRuleSet(this RoslynExportProfileResponse roslynProfileExporter)
        {
            if (roslynProfileExporter == null)
            {
                return null;
            }

            try
            {
                var tempRuleSetFilePath = Path.GetTempFileName();

                File.WriteAllText(tempRuleSetFilePath, roslynProfileExporter.Configuration.RuleSet.OuterXml);
                var ruleSet = RuleSet.LoadFromFile(tempRuleSetFilePath);
                File.Delete(tempRuleSetFilePath);

                return ruleSet;
            }
            catch
            {
                return null;
            }
        }

        public static QualityProfile ToQualityProfile(this RuleSet ruleset, Language language)
        {
            if (ruleset == null)
            {
                return null;
            }

            var rules = ruleset.Rules
                .Where(r => r.Action != RuleAction.None)
                .Select(r => new SonarRule(r.Id));

            return new QualityProfile(language, rules);
        }
    }
}
