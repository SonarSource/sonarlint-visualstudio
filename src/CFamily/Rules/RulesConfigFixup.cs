/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2022 SonarSource SA
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
    /// </remarks>
    internal interface IRulesConfigFixup
    {
        RulesSettings Apply(RulesSettings input);
    }

    internal class RulesConfigFixup : IRulesConfigFixup
    {
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

        private static readonly IReadOnlyDictionary<string, string> fullLegacyToNewKeyMap;

        static RulesConfigFixup()
        {
            var mapWithLanguagePrefixes = new Dictionary<string, string>();
            foreach (var partial in partialLegacyToNewKeyMap)
            {
                mapWithLanguagePrefixes[$"{SonarLanguageKeys.C}:{partial.Key}"] = $"{SonarLanguageKeys.C}:{partial.Value}";
                mapWithLanguagePrefixes[$"{SonarLanguageKeys.CPlusPlus}:{partial.Key}"] = $"{SonarLanguageKeys.CPlusPlus}:{partial.Value}";
            }
            fullLegacyToNewKeyMap = mapWithLanguagePrefixes;
        }

        /// <summary>
        /// Translates any legacy rule keys in the input to new Sxxx rule keys
        /// </summary>
        public RulesSettings Apply(RulesSettings input)
        {
            foreach (var inputKey in input.Rules.Keys.ToArray())
            {
                if (fullLegacyToNewKeyMap.TryGetValue(inputKey, out var newKey))
                {
                    var inputConfig = input.Rules[inputKey];
                    input.Rules.Remove(inputKey);

                    // There might already be a setting with the new key. If so,
                    // we'll keep it and drop the legacy key setting.
                    if (!input.Rules.ContainsKey(newKey))
                    {
                        input.Rules[newKey] = inputConfig;
                    }
                }
            }

            return input;
        }
    }
}
