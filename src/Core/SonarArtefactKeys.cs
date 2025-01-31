﻿/*
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

using System;

namespace SonarLint.VisualStudio.Core
{
    // Concepts:
    // The Sonar plugin model defines various types of artefact, all of which have
    // unique identifiers:
    //
    // * plugin : a container for different types of extension
    //
    // * language : A language is defined in a single plugin but can be consumed by multiple
    //              e.g. the CSharp language is defined in the SonarC# plugin, with additional
    //              rules for the language being defined in the SonarC# Security plugin.
    //
    // * repository: a container for rule definitions. A repository provides rules for a single language.
    //
    // * rule definition: part of a single repository. The full rule id is given by "[repo id]:[rule id]"

    public static class SonarRuleRepoKeys
    {
        public const string JavaScript = "javascript";
        public const string TypeScript = "typescript";
        public const string Css = "css";
        public const string C = "c";
        public const string Cpp = "cpp";
        public const string TSql = "tsql";

        public const string Secrets = "secrets";

        public const string CSharpSecurityRules = "roslyn.sonaranalyzer.security.cs";
        public const string CSharpRules = "csharpsquid";
        public const string VBNetRules = "vbnet";

        public const string JsSecurityRules = "jssecurity";
        public const string TsSecurityRules = "tssecurity";

        // The HTML plugin uses the repo key "Web".
        // See https://github.com/SonarSource/sonar-html/blob/155f558546b4035165d1e4e2ba62f22e3cf7d0cc/sonar-html-plugin/src/main/java/org/sonar/plugins/html/rules/HtmlRulesDefinition.java#L31
        public const string HtmlRules = "Web";

        public static readonly StringComparer RepoKeyComparer = StringComparer.Ordinal;

        public static bool AreEqual(string repoKey1, string repoKey2) =>
            string.Equals(repoKey1, repoKey2, StringComparison.Ordinal);
    }
}
