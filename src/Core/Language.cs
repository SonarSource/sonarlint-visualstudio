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
        public readonly static Language Unknown = new Language();
        public readonly static Language CSharp = new Language("CSharp", CoreStrings.CSharpLanguageName, "sonarlint_csharp.globalconfig", SonarQubeLanguage.CSharp,
            // Support for C# hotspots. No need to special-case the VB.NET hotspots, as their repo name is identical to the one on rules.sonarsource.com
            new RepoInfo("csharpsquid", "csharp"), new RepoInfo("roslyn.sonaranalyzer.security.cs", "csharp"));
        public readonly static Language VBNET = new Language("VB", CoreStrings.VBNetLanguageName, "sonarlint_vb.globalconfig", SonarQubeLanguage.VbNet, new("vbnet"));
        public readonly static Language Cpp = new Language("C++", CoreStrings.CppLanguageName, null, SonarQubeLanguage.Cpp, new("cpp"));
        public readonly static Language C = new Language("C", "C", null, SonarQubeLanguage.C, new("c"));
        public readonly static Language Js = new Language("Js", "JavaScript", null, SonarQubeLanguage.Js, new("javascript"), new("jssecurity", "javascript"));
        public readonly static Language Ts = new Language("Ts", "TypeScript", null, SonarQubeLanguage.Ts, new("typescript"), new("tssecurity", "typescript"));
        public readonly static Language Css = new Language("Css", "CSS", null, SonarQubeLanguage.Css, new("css"));
        public readonly static Language
            Html = new Language("Html", "HTML", null, SonarQubeLanguage.Html, new("Web", "html")); //See https://github.com/SonarSource/sonarlint-visualstudio/issues/4586.
        public readonly static Language Secrets = new Language("Secrets", "Secrets", null, SonarQubeLanguage.Secrets, new("secrets"));
        public readonly static Language TSql = new Language("TSql", "T-SQL", null, SonarQubeLanguage.TSql, new("tsql"));

        /// <summary>
        /// Returns the language for the specified language key, or null if it does not match a known language
        /// </summary>
        public static Language GetLanguageFromLanguageKey(string languageKey) => KnownLanguages.FirstOrDefault(l => languageKey.Equals(l.ServerLanguage.Key, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Returns the language for the specified repository key, or null if it does not match a known language
        /// </summary>
        public static Language GetLanguageFromRepositoryKey(string repoKey)
        {
            repoKeyToLanguage.TryGetValue(repoKey, out Language language);

            return language;
        }

        /// <summary>
        /// Returns the Sonar analyzer repository for the specified language key, or null if one could not be found
        /// </summary>
        public static string GetSonarRepoKeyFromLanguage(Language language)
        {
            if (language?.ServerLanguage?.Key == null)
            {
                return null;
            }
            var match = repoKeyToLanguage.FirstOrDefault(x => !x.Key.Contains("security") && x.Value.ServerLanguage.Key == language.ServerLanguage.Key);
            return match.Key ?? null;
        }

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
        /// Matches the repository name to its language object counterpart.
        /// </summary>
        private static readonly Dictionary<string, Language> repoKeyToLanguage = new Dictionary<string, Language>()
        {
            { "csharpsquid", CSharp },
            { "roslyn.sonaranalyzer.security.cs", CSharp },
            { "vbnet", VBNET },
            { "cpp", Cpp },
            { "c", C },
            { "javascript", Js },
            { "jssecurity", Js },
            { "typescript", Ts },
            { "tssecurity", Ts },
            { "css", Css },
            { "Web", Html },
            { "secrets", Secrets },
            { "tsql", TSql },
        };

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
            SecurityRepoInfo = securityRepoInfo != null && securityRepoInfo.Value == default ? throw new ArgumentException(nameof(securityRepoInfo)) : repoInfo;
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
    }
}
