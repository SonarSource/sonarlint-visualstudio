//-----------------------------------------------------------------------
// <copyright file="Language.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarLint.VisualStudio.Integration.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

namespace SonarLint.VisualStudio.Integration
{
    /// <summary>
    /// Represents a programming language. Implements <seealso cref="IEquatable{T}"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <seealso cref="Language"/>s are equal if they have the same <see cref="Id"/> and <see cref="ProjectType"/>.
    /// </para>
    /// <para>
    /// This class is safe for use as a key in collection classes. E.g., <seealso cref="IDictionary{TKey, TValue}"/>.
    /// </para>
    /// </remarks>
    [DebuggerDisplay("{Name} (ID: {Id}, ProjectType: {ProjectType}, IsSupported: {IsSupported})")]
    [TypeConverter(typeof(LanguageConverter))]
    public sealed class Language : IEquatable<Language>
    {
        public readonly static Language Unknown = new Language();
        public readonly static Language CSharp = new Language("CSharp", Strings.CSharpLanguageName, ProjectSystemHelper.CSharpProjectKind);
        public readonly static Language VBNET = new Language("VB", Strings.VBNetLanguageName, ProjectSystemHelper.VbProjectKind);

        /// <summary>
        /// A stable identifer for this language.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// The language display name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The VS project GUID for this language.
        /// </summary>
        public Guid ProjectType { get; }

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
                // We don't support VB.NET as the corresponding VB SonarQube server plugin hasn't been
                // updated to support the connected mode.
                return new[] { CSharp };
            }
        }

        /// <summary>
        /// All known languages.
        /// </summary>
        public static IEnumerable<Language> KnownLanguages
        {
            get
            {
                return new[] { CSharp, VBNET };
            }
        }

        /// <summary>
        /// Private constructor reserved for the <seealso cref="Unknown"/>.
        /// </summary>
        private Language()
        {
            this.Id = string.Empty;
            this.Name = Strings.UnknownLanguageName;
            this.ProjectType = Guid.Empty;
        }

        public Language(string id, string name, string projectTypeGuid)
            : this(id, name, new Guid(projectTypeGuid))
        {
        }

        public Language(string id, string name, Guid projectType)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentNullException(nameof(id));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            this.Id = id;
            this.Name = name;
            this.ProjectType = projectType;
        }

        public static Language ForProject(EnvDTE.Project dteProject)
        {
            if (dteProject == null)
            {
                throw new ArgumentNullException(nameof(dteProject));
            }

            Guid projectKind;
            if (!Guid.TryParse(dteProject.Kind, out projectKind))
            {
                return Unknown;
            }

            return KnownLanguages.FirstOrDefault(x => x.ProjectType == projectKind) ?? Unknown;
        }

        #region IEquatable<Language> and Equals

        public bool Equals(Language other)
        {
            if (ReferenceEquals(other, this))
            {
                return true;
            }

            return other != null
                && other.Id == this.Id
                && other.ProjectType == this.ProjectType;
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
