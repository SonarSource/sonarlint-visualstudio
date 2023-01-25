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

namespace ExtractRuleDescFromJson;

/// <summary>
/// Extracts the description for each rule from the json file
/// and save each one in a separate file.
/// </summary>
/// <remarks>
/// It's assumed that each language has its own folder
/// e.g. 
///      /cs
///         /S100.desc
///         /S101.desc
///         ...
///      /vbnet
///         /S100.desc
///         ...
///         
/// We also expect the rule description to be an HTML fragment, but the extractor
/// doesn't actually care - we just treat it as text.
/// </remarks>
internal class RuleDescExtractor
{
    public static void ProcessFile(Context context)
        => new RuleDescExtractor(context).ProcessFile();

    private readonly Context context;

    private RuleDescExtractor(Context context) => this.context = context;

    private void ProcessFile()
    {
        PrepareDestinationDirectory();

        Logger.LogMessage($"Processing file: {context.RuleJsonFilePath}");

        var rules = LoadRules(context.RuleJsonFilePath);

        Logger.LogMessage($"Processing rules: ");
        foreach (var rule in rules)
        {
            ProcessRule(rule);
        }
    }

    // Ensure the directory exists but does not have any .desc files
    private void PrepareDestinationDirectory()
    {
        if (!Directory.Exists(context.DestinationDirectory))
        {
            Logger.LogMessage("Creating destination directory: " + context.DestinationDirectory);
            Directory.CreateDirectory(context.DestinationDirectory);
        }
        else
        {
            Logger.LogMessage("Removing existing .desc files from destination directory: " + context.DestinationDirectory);
            var descFiles = Directory.GetFiles(context.DestinationDirectory, "*.desc", SearchOption.TopDirectoryOnly);
            var deleteCount = 0; 
            foreach(var file in descFiles)
            {
                File.Delete(file);
                deleteCount++;
            }
            Logger.LogMessage("  Done. Number of files deleted: " + deleteCount);
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
            // TODO: clean up HTML?
            SaveRuleFile(rule);
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error processing rule. Rule key: {rule.Key}, file: {context.RuleJsonFilePath}, {ex.Message}");
        }
    }

    private void SaveRuleFile(Rule rule)
    {
        var fullPath = CalculateRuleFileName(rule);
        Logger.LogPartialMessage($" {Path.GetFileName(fullPath)}");
        File.WriteAllText(fullPath, rule.Description);
    }

    private string CalculateRuleFileName(Rule rule)
    {
        // e.g. "S123.desc"
        var colonPos = rule.Key.IndexOf(':');
        if (colonPos == -1)
        {
            throw new InvalidOperationException("Invalid rule key: " + rule.Key);
        }

        var ruleKeyWithoutLanguage = rule.Key.Substring(colonPos + 1);
        var fileName = ruleKeyWithoutLanguage + ".desc";
        return Path.Combine(context.DestinationDirectory, fileName);
    }
}
