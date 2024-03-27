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

using System.Text.Json;
using System.Text.Json.Serialization;
using SonarLint.VisualStudio.Rules;

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
            foreach (var file in descFiles)
            {
                File.Delete(file);
                deleteCount++;
            }
            Logger.LogMessage("  Done. Number of files deleted: " + deleteCount);
        }
    }

    private static IEnumerable<PluginRule> LoadRules(string file)
    {
        var options = new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter() },
            PropertyNameCaseInsensitive = true
        };

        var json = File.ReadAllText(file);
        Logger.LogMessage(json.Substring(100));
        return JsonSerializer.Deserialize<PluginRule[]>(json, options)
            ?? Array.Empty<PluginRule>();
    }

    private void ProcessRule(PluginRule pluginRule)
    {
        try
        {
            var descAsxml = HtmlXmlCompatibilityHelper.EnsureHtmlIsXml(pluginRule.Description);

            var slvsRule = new RuleInfo(
                pluginRule.Language?.ToLower() ?? throw new ArgumentNullException("language"),
                pluginRule.Key ?? throw new ArgumentNullException("key"),
                descAsxml,
                pluginRule.Name ?? throw new ArgumentNullException("name"),
                ConvertPluginSeverity(pluginRule.DefaultSeverity),
                ConvertPluginIssueType(pluginRule.Type),
                Convert.ToBoolean(pluginRule.IsActiveByDefault),
                pluginRule.Tags ?? Array.Empty<string>(),
                pluginRule.DescriptionSections?.Select(x => new SonarLint.VisualStudio.Rules.DescriptionSection(x.Key,
                    HtmlXmlCompatibilityHelper.EnsureHtmlIsXml(x.HtmlContent),
                    x.Context != null
                        ? new SonarLint.VisualStudio.Rules.Context(x.Context.Key, x.Context.DisplayName)
                        : null)).ToList(),
                pluginRule.EducationPrinciples,
                null,
                pluginRule.CleanCodeAttribute,
                pluginRule.DefaultImpacts
                );

            SaveRuleFile(slvsRule);
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error processing rule. Rule key: {pluginRule.Key}, file: {context.RuleJsonFilePath}, {ex.Message}");
        }
    }

    private static RuleIssueSeverity ConvertPluginSeverity(string? pluginSeverity)
        => pluginSeverity switch
        {
            "MINOR" => RuleIssueSeverity.Minor,
            "MAJOR" => RuleIssueSeverity.Major,
            "INFO" => RuleIssueSeverity.Info,
            "BLOCKER" => RuleIssueSeverity.Blocker,
            "CRITICAL" => RuleIssueSeverity.Critical,
            _ => throw new ArgumentException("Invalid enum value for pluginSeverity" + pluginSeverity, nameof(pluginSeverity)),
        };

    private RuleIssueType ConvertPluginIssueType(string? pluginIssueType)
        => pluginIssueType switch
        {
            "SECURITY_HOTSPOT" => RuleIssueType.Hotspot,
            "VULNERABILITY" => RuleIssueType.Vulnerability,
            "BUG" => RuleIssueType.Bug,
            "CODE_SMELL" => RuleIssueType.CodeSmell,
            _ => throw new ArgumentException("Invalid enum value for pluginIssueType" + pluginIssueType, nameof(pluginIssueType)),
        };

    private void SaveRuleFile(RuleInfo slvsRuleInfo)
    {
        var fullPath = CalculateRuleFileName(slvsRuleInfo);
        Logger.LogPartialMessage($" {Path.GetFileName(fullPath)}");

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        var data = JsonSerializer.Serialize(slvsRuleInfo, options);
        File.WriteAllText(fullPath, data);
    }

    private string CalculateRuleFileName(RuleInfo slvsRule)
    {
        // e.g. "S123.desc"
        var colonPos = slvsRule.FullRuleKey.IndexOf(':');
        if (colonPos == -1)
        {
            throw new InvalidOperationException("Invalid rule key: " + slvsRule.FullRuleKey);
        }

        var ruleKeyWithoutRepoKey = slvsRule.FullRuleKey.Substring(colonPos + 1);
        var fileName = ruleKeyWithoutRepoKey + ".json";
        return Path.Combine(context.DestinationDirectory, fileName);
    }
}
