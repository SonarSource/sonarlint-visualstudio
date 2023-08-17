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
using System.Linq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Hotspots;

namespace SonarLint.VisualStudio.CFamily.Rules
{
    /// <summary>
    /// Applies any necessary fix-ups to the rules configuration e.g. translating legacy rule keys
    /// </summary>
    /// <remarks>
    /// Legacy rule keys
    /// ----------------
    /// The CFamily analyzer stopped using rule keys in the legacy "friendly name" style in v6.2:
    /// all rule keys are now in the "Sxxx" format.
    /// 
    /// There are two scenarios we need to handle:
    /// 1) the user is using a version of SonarQube that has a pre-v6.2 version of the CFamily analyzer,
    ///    so that in Connected Mode the Quality Gate will return legacy keys; and
    /// 2) the user's settings.json file contains entries using a legacy key
    /// 
    /// Excluded rule keys
    /// ------------------
    /// There are some rules we don't want to run in SonarLint:
    /// * rules that need all of the files in the project to produce accurate results, and
    /// * security hotspots.
    /// These should never be run, even if they are explicitly enabled in custom settings.
    /// </remarks>
    internal interface IRulesConfigFixup
    {
        RulesSettings Apply(RulesSettings input, IHotspotAnalysisConfiguration hotspotAnalysisConfiguration);
    }

    internal class RulesConfigFixup : IRulesConfigFixup
    {
        internal static readonly string[] ExcludedRulesKeys = new string[] {
            // Project-level:
            "cpp:S5536", "c:S5536",
            "cpp:S4830", "c:S4830",
            "cpp:S5527", "c:S5527",
        };

        internal static readonly string[] HotspotRulesKeys = new[]
        {
            // Security hotspots:
            "cpp:S5801", "c:S5801",
            "cpp:S5814", "c:S5814",
            "cpp:S5815", "c:S5815",
            "cpp:S5816", "c:S5816",
            "cpp:S5824", "c:S5824",
            "cpp:S2612", "c:S2612",
            "cpp:S5802", "c:S5802",
            "cpp:S5849", "c:S5849",
            "cpp:S5982", "c:S5982",
            "cpp:S5813", "c:S5813",
            "cpp:S5332", "c:S5332",
            "cpp:S2068", "c:S2068",
            "cpp:S2245", "c:S2245",
            "cpp:S5443", "c:S5443",
            "cpp:S5042", "c:S5042",
            "cpp:S4790", "c:S4790",
            "cpp:S1313", "c:S1313",
            "cpp:S6069", "c:S6069",
        };

        private static readonly IReadOnlyDictionary<string, string> partialLegacyToNewKeyMap = new Dictionary<string, string>
        {
            // Permalink to v6.32 mapping: https://github.com/SonarSource/sonar-cpp/blob/c51c7ccb23e32f587a543a2e4b08f10e92daf2a7/sonar-cfamily-plugin/src/main/java/com/sonar/cpp/plugin/AbstractRulesDefinition.java#L35
            // This data isn't available as metadata so we have a hard-coded mapping.
            { "C99CommentUsage", "S787"},
            { "SideEffectInRightHandSideOfLogical", "S912"},
            { "FunctionEllipsis", "S923"},
            { "SingleGotoOrBreakPerIteration", "S924"},
            { "ExceptionSpecificationUsage", "S2303"},
            { "PPDirectiveIndentation", "S1915"},
            { "NamespaceName", "S2304"},
            { "NonReentrantFunction", "S1912"},
            { "PPMacroName", "S1543" },
            { "ElseIfWithoutElse","S126" },
            { "SideEffectInSizeOf","S922" },
            { "NonEmptyCaseWithoutBreak","S128" },
            { "AssignmentInSubExpression", "S1121" },
            { "OctalConstantAndSequence", "S1314" },
            { "PPNonStandardInclude", "S2305" },
            { "SizeofSizeof", "S1913" },
            { "PPErrorDirectiveReached", "S1914" },
            { "UnnamedNamespaceInHeader", "S1000" },
            { "LogicalExpressionOperands", "S868" },
            { "PPIncludeCtime", "S1052" },
            { "PPIncludeCstdio", "S1055" },
            { "SingleDeclarationPerStatement", "S1659" },
            { "UsingDirective", "S1001" },
            { "EmptyThrowOutsideHandler", "S1039" },
            { "EllipsisHandlerNotLast", "S1046" },
            { "LiteralSuffix", "S818" },
            { "ExceptionInDestructor", "S1048" },
            { "IncAndDecMixedWithOtherOperators", "S881" },
            { "NarrowAndWideStringConcat", "S817" },
            { "Union", "S953" },
            { "GlobalMainFunction","S998" },
            { "GotoLabelInNestedBlock", "S1909" },
            { "PPIncludeNotAtTop","S954" },
            { "PPIncludeTime", "S991" },
            { "TrigraphUsage", "S797" },
            { "ContinueUsage", "S909" },
            { "LineLength", "S103" },
            { "FileLoc", "S104" },
            { "GotoUsage", "S907" },
            { "IdentifierLongerThan31", "S799" },
            { "GlobalNamespaceMembers", "S997" },
            { "PPIncludeNonStandardCharacters","S955" },
            { "BackJumpWithGoto", "S999" },
            { "FileComplexity", "S1908" },
            { "TabCharacter", "S105" },
            { "DigraphUsage", "S798" },
            { "InvalidEscapeSequence", "S796" },
            { "ObsoletePosixFunction", "S1911" },
            { "PPIncludeSignal", "S987" },
            { "PPBackslashNotLastCharacter", "S1916" },
            { "ClassComplexity", "S1311" },
            { "SwitchLabelPlacement", "S916" },
            { "PPIncludeStdio", "S988" },
            { "FunctionComplexity", "S1541" },
            { "CommentMixedStyles", "S1917" },
            { "OneStatementPerLine", "S122" },
            { "CommaAndOrOverloaded", "S919" },
            { "CommentedCode", "S125" },
            { "FunctionSinglePointOfExit", "S1005" },
            { "PPIncludeCHeader","S1051" },
            { "EnumPartialInitialization", "S841" },
            { "UnaryAndOverloaded", "S877" },
            { "ParsingError", "S2260" },
            { "SwitchWithoutDefault", "S131" },
            { "PPStringifyAndPastingUsage", "S968" },
            { "PPUndefUsage", "S959" },
            { "ClassName", "S101" },
            { "EmptyCompoundStatement", "S108" },
            { "PPDefineOrUndefFromBlock","S958" },
            { "PPBadIncludeForm", "S956" }
        };

        internal static readonly IReadOnlyDictionary<string, string> fullLegacyToNewKeyMap = CalculateFullKeyMap();

        private static IReadOnlyDictionary<string, string> CalculateFullKeyMap()
        {
            var mapWithLanguagePrefixes = new Dictionary<string, string>();
            foreach (var partial in partialLegacyToNewKeyMap)
            {
                mapWithLanguagePrefixes[$"{SonarLanguageKeys.C}:{partial.Key}"] = $"{SonarLanguageKeys.C}:{partial.Value}";
                mapWithLanguagePrefixes[$"{SonarLanguageKeys.CPlusPlus}:{partial.Key}"] = $"{SonarLanguageKeys.CPlusPlus}:{partial.Value}";
            }
            return mapWithLanguagePrefixes;
        }

        private readonly ILogger logger;

        public RulesConfigFixup(ILogger logger) => this.logger = logger ?? throw new System.ArgumentNullException(nameof(logger));

        /// <summary>
        /// Translates any legacy rule keys in the input to new Sxxx rule keys
        /// </summary>
        public RulesSettings Apply(RulesSettings input, IHotspotAnalysisConfiguration hotspotAnalysisConfiguration)
        {
            /*

            We're making a shallow copy of the list of rules. If we modify the original list, any exclusions we 
            add could end up be saved in the user settings.json file(if that is where the custom rules
            came from).
            
            Modifying the saved user settings.json would be a problem in the following scenario:
                * the file has a legacy key settings e.g.cpp:C99CommentUsage
                * the user has multiple VS instances with "old" and "new" SonarLint instances installed
            e.g.they still have an instance oF VS2015 / 2017 they need to use
                
            In that case, we don't want to update the legacy keys in the settings file since it would
            re - enable the rules in the "old" version.

            However, _not_ updating the legacy keys in the file has a different issue: the file could
            contain both old and new keys e.g.
                * settings file has legacy rule key e.g.cpp:C99CommentUsage(set to "On")
                * user disables the corresponding "new" rule S787.
            In that case, we'll warn in the output window that the legacy setting is being ignored.

            */
            
            var modifiedSettings = new RulesSettings
            {
                Rules = new Dictionary<string, RuleConfig>(input.Rules, input.Rules.Comparer)
            };

            TranslateLegacyRuleKeys(modifiedSettings);
            DisableExcludedRules(modifiedSettings, hotspotAnalysisConfiguration.IsEnabled());

            return modifiedSettings;
        }

        private void TranslateLegacyRuleKeys(RulesSettings settings)
        {
            foreach (var inputKey in settings.Rules.Keys.ToArray())
            {
                if (fullLegacyToNewKeyMap.TryGetValue(inputKey, out var newKey))
                {
                    var inputConfig = settings.Rules[inputKey];
                    settings.Rules.Remove(inputKey);

                    // There might already be a setting with the new key. If so, we'll keep it and drop the legacy key setting.
                    if (settings.Rules.ContainsKey(newKey))
                    {
                        logger.WriteLine(Resources.DuplicateLegacyAndNewRuleKey, inputKey, newKey);
                    }
                    else
                    {
                        logger.LogVerbose($"[CFamily] Translating legacy rule key: {inputKey} -> {newKey}");
                        settings.Rules[newKey] = inputConfig;
                    }
                }
            }
        }

        /// <summary>
        /// Marks all excluded rules as disabled, adding them to the settings if necessary
        /// </summary>
        private void DisableExcludedRules(RulesSettings settings, bool hotspotsEnabled)
        {
            ICollection<string> disabledRules = ExcludedRulesKeys;

            if (!hotspotsEnabled)
            {
                disabledRules = disabledRules.Concat(HotspotRulesKeys).ToList();
            }
            
            logger.WriteLine(Resources.RulesUnavailableInSonarLint, string.Join(", ", disabledRules));

            foreach (var key in disabledRules)
            {
                settings.Rules[key] = new RuleConfig { Level = RuleLevel.Off };
            }
        }

    }
}
