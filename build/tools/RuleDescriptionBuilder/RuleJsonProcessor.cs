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

using System.Text.Json;

namespace RuleDescriptionBuilder
{
    internal class RuleJsonProcessor
    {
        public static void ProcessFile(Context context)
            => new RuleJsonProcessor(context).ProcessFile();

        private readonly Context context;

        private RuleJsonProcessor(Context context) => this.context = context;

        private void ProcessFile()
        {
            Logger.LogMessage($"Processing file: {context.RuleJsonFilePath}");

            var rules = LoadRules(context.RuleJsonFilePath);

            foreach (var rule in rules)
            {
                ProcessRule(rule);
            }
        }

        private static IEnumerable<Rule> LoadRules(string file)
        {
            var json = File.ReadAllText(file);
            return JsonSerializer.Deserialize<Rule[]>(json)
                ?? Array.Empty<Rule>();
        }

        private void ProcessRule(Rule rule)
        {
            try
            {
                var xaml = SimpleRuleHtmlToXamlConverter.Convert(rule);
                SaveRuleXaml(rule, xaml);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error processing rule. Rule key: {rule.Key}, file: {context.RuleJsonFilePath}, {ex.Message}");
            }
        }

        private void SaveRuleXaml(Rule rule, string xaml)
        {
            var fullPath = CalculateRuleFileName(rule);
            if (File.Exists(fullPath))
            {
                Logger.LogMessage($"    Deleting existing rule file: {fullPath}");
                File.Delete(fullPath);
            }

            Logger.LogMessage($"    Writing rule file: {fullPath}");
            File.WriteAllText(fullPath, xaml);
        }

        private string CalculateRuleFileName(Rule rule)
        {
            // e.g. "css_S123.xaml"
            var fileName = rule.Key.Replace(":", "_") + ".xaml";
            return Path.Combine(context.DestinationDirectory, fileName);
        }
    }
}
