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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
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
        public readonly static Language CSharp = new Language("CSharp", CoreStrings.CSharpLanguageName, "csharp.ruleset", SonarQubeLanguage.CSharp);
        public readonly static Language VBNET = new Language("VB", CoreStrings.VBNetLanguageName, "vb.ruleset", SonarQubeLanguage.VbNet);
        public readonly static Language Cpp = new Language("C++", CoreStrings.CppLanguageName, "_cpp_settings.json", SonarQubeLanguage.Cpp);
        public readonly static Language C = new Language("C", "C", "_c_settings.json", SonarQubeLanguage.C);

        /// <summary>
        /// Returns the language for the specified language key, or null if it does not match a known language
        /// </summary>
        public static Language GetLanguageFromLanguageKey(string languageKey) =>
            KnownLanguages.FirstOrDefault(l => languageKey.Equals(l.ServerLanguage.Key, System.StringComparison.OrdinalIgnoreCase));

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

        /// <summary>
        /// Returns whether or not this language is a supported project language.
        /// </summary>
        public bool IsSupported => SupportedLanguages.Contains(this);

        /// <summary>
        /// All languages which are supported for project binding.
        /// </summary>
        public static IEnumerable<Language> SupportedLanguages
        {
            get
            {
                return new[] { CSharp, VBNET, Cpp, C };
            }
        }

        /// <summary>
        /// All known languages.
        /// </summary>
        public static IEnumerable<Language> KnownLanguages
        {
            get
            {
                return new[] { CSharp, VBNET, Cpp, C };
            }
        }

        /// <summary>
        /// Private constructor reserved for the <seealso cref="Unknown"/>.
        /// </summary>
        private Language()
        {
            this.Id = string.Empty;
            this.Name = CoreStrings.UnknownLanguageName;
            this.FileSuffixAndExtension = string.Empty;
        }

        public Language(string id, string name, string fileSuffix, SonarQubeLanguage serverLanguage)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentNullException(nameof(id));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (string.IsNullOrWhiteSpace(fileSuffix))
            {
                throw new ArgumentNullException(nameof(fileSuffix));
            }

            this.Id = id;
            this.Name = name;
            this.FileSuffixAndExtension = fileSuffix;
            this.ServerLanguage = serverLanguage ?? throw new ArgumentNullException(nameof(serverLanguage));
        }

        #region IEquatable<Language> and Equals

        public bool Equals(Language other)
        {
            if (ReferenceEquals(other, this))
            {
                return true;
            }

            return other != null
                && other.Id == this.Id;
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as Language);
        }

        public override int GetHashCode()
        {
            return this.Id.GetHashCode();
        }

        #endregion
    }
}
