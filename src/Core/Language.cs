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

using System.ComponentModel;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Core
{
    /// <summary>
    /// Represents a programming language for which connected mode is supported. Implements <seealso cref="IEquatable{T}"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <seealso cref="Language"/>s are equal if they have the same <see cref="Id"/> and <see cref="ProjectType"/>.
    /// </para>
    /// <para>
    /// This class is safe for use as a key in collection classes. E.g., <seealso cref="IDictionary{TKey, TValue}"/>.
    /// </para>
    /// </remarks>
    [DebuggerDisplay("{Name} (ID: {Id}, IsSupported: {IsSupported})")]
    [TypeConverter(typeof(LanguageConverter))]
    public sealed class Language : IEquatable<Language>
    {
        private static readonly RepoInfo CSharpRepo = new("csharpsquid", "csharp");
        private static readonly RepoInfo CSharpSecurityRepo = new("roslyn.sonaranalyzer.security.cs", "csharp");
        private static readonly RepoInfo VbNetRepo = new("vbnet");
        private static readonly RepoInfo CppRepo = new("cpp");
        private static readonly RepoInfo CRepo = new("c");
        private static readonly RepoInfo JsRepo = new("javascript");
        private static readonly RepoInfo JsSecurityRepo = new("jssecurity", "javascript");
        private static readonly RepoInfo TsRepo = new("typescript");
        private static readonly RepoInfo TsSecurityRepo = new("tssecurity", "typescript");
        private static readonly RepoInfo CssRepo = new("css");
        private static readonly RepoInfo HtmlRepo = new("Web", "html"); //See https://github.com/SonarSource/sonarlint-visualstudio/issues/4586.
        private static readonly RepoInfo SecretsRepo = new("secrets");
        private static readonly RepoInfo TsqlRepo = new("tsql");

        public static readonly Language Unknown = new();
        public static readonly Language CSharp = new("CSharp", CoreStrings.CSharpLanguageName, "sonarlint_csharp.globalconfig", SonarQubeLanguage.CSharp, CSharpRepo, CSharpSecurityRepo);
        public static readonly Language VBNET = new("VB", CoreStrings.VBNetLanguageName, "sonarlint_vb.globalconfig", SonarQubeLanguage.VbNet, VbNetRepo);
        public static readonly Language Cpp = new("C++", CoreStrings.CppLanguageName, null, SonarQubeLanguage.Cpp, CppRepo);
        public static readonly Language C = new("C", "C", null, SonarQubeLanguage.C, CRepo);
        public static readonly Language Js = new("Js", "JavaScript", null, SonarQubeLanguage.Js, JsRepo, JsSecurityRepo);
        public static readonly Language Ts = new("Ts", "TypeScript", null, SonarQubeLanguage.Ts, TsRepo, TsSecurityRepo);
        public static readonly Language Css = new("Css", "CSS", null, SonarQubeLanguage.Css, CssRepo);
        public static readonly Language Html = new("Html", "HTML", null, SonarQubeLanguage.Html, HtmlRepo);
        public static readonly Language Secrets = new("Secrets", "Secrets", null, SonarQubeLanguage.Secrets, SecretsRepo);
        public static readonly Language TSql = new("TSql", "T-SQL", null, SonarQubeLanguage.TSql, TsqlRepo);

        /// <summary>
        /// Returns the language for the specified language key, or null if it does not match a known language
        /// </summary>
        public static Language GetLanguageFromLanguageKey(string languageKey) => KnownLanguages.FirstOrDefault(l => languageKey.Equals(l.ServerLanguage.Key, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Returns the language for the specified repository key, or null if it does not match a known language
        /// </summary>
        public static Language GetLanguageFromRepositoryKey(string repoKey) => KnownLanguages.SingleOrDefault(lang => lang.HasRepoKey(repoKey));

        /// <summary>
        /// Returns the Sonar analyzer repository for the specified language key, or null if one could not be found
        /// </summary>
        public static string GetSonarRepoKeyFromLanguage(Language language) => KnownLanguages.SingleOrDefault(x => x.Id == language.Id)?.RepoInfo.Key;

        /// <summary>
        /// A stable identifier for this language.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Object containing the server-side description of the language as used by SonarQube/Cloud
        /// </summary>
        /// <remarks>The server-side language key is the stable id used server-side to identify a language. Ideally this would have been used as the
        /// Id here originally. However, it wasn't and we can't easily change the Id values now since they are serialized in the
        /// solution-level binding file.</remarks>
        public SonarQubeLanguage ServerLanguage { get; }

        /// <summary>
        /// The language display name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Suffix and extension added to the language-specific rules configuration file for the language
        /// </summary>
        /// <remarks>e.g. for ruleset-based languages this will be a language identifier + ".ruleset"</remarks>
        public string FileSuffixAndExtension { get; }

        public RepoInfo RepoInfo { get; }

        /// <summary>
        /// The repository info for the security rules (i.e. hotspots) for this language
        /// </summary>
        public RepoInfo? SecurityRepoInfo { get; }

        /// <summary>
        /// Returns whether or not this language is a supported project language.
        /// </summary>
        public bool IsSupported => KnownLanguages.Contains(this);

        /// <summary>
        /// All known languages.
        /// </summary>
        public static IEnumerable<Language> KnownLanguages
        {
            get
            {
                return new[] { CSharp, VBNET, Cpp, C, Js, Ts, Css, Html, Secrets, TSql };
            }
        }

        /// <summary>
        /// Private constructor reserved for the <seealso cref="Unknown"/>.
        /// </summary>
        private Language()
        {
            Id = string.Empty;
            Name = CoreStrings.UnknownLanguageName;
            FileSuffixAndExtension = string.Empty;
        }

        public Language(
            string id,
            string name,
            string fileSuffix,
            SonarQubeLanguage serverLanguage,
            RepoInfo repoInfo,
            RepoInfo? securityRepoInfo = null)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentNullException(nameof(id));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            Id = id;
            Name = name;
            FileSuffixAndExtension = fileSuffix;
            ServerLanguage = serverLanguage ?? throw new ArgumentNullException(nameof(serverLanguage));
            RepoInfo = repoInfo == default ? throw new ArgumentException(nameof(repoInfo)) : repoInfo;
            SecurityRepoInfo = securityRepoInfo != null && securityRepoInfo.Value == default ? throw new ArgumentException(nameof(securityRepoInfo)) : securityRepoInfo;
        }

        #region IEquatable<Language> and Equals

        public bool Equals(Language other)
        {
            if (ReferenceEquals(other, this))
            {
                return true;
            }

            return other != null
                   && other.Id == Id;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Language);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        #endregion

        public override string ToString() => Name;

        public bool HasRepoKey(string repoKey) => RepoInfo.Key == repoKey || SecurityRepoInfo?.Key == repoKey;
    }
}
