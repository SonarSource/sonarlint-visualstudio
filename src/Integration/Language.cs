using SonarLint.VisualStudio.Integration.Resources;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SonarLint.VisualStudio.Integration
{
    [DebuggerDisplay("{Name} (ServerKey: {ServerKey}, ProjectType: {ProjectType}, IsSupported: {IsSupported})")]
    public sealed class Language : IEquatable<Language>
    {
        public readonly static Language CSharp = new Language("cs", Strings.CSharpLanguageName, ProjectSystemHelper.CSharpProjectKind);
        public readonly static Language VBNET = new Language("vbnet", Strings.VBNetLanguageName, ProjectSystemHelper.VbProjectKind);

        public string ServerKey { get; }

        public string Name { get; }

        public Guid ProjectType { get; set; }

        public bool IsSupported => SupportedLanguages.Contains(this);

        public static IEnumerable<Language> SupportedLanguages
        {
            get
            {
                yield return CSharp;
                // We don't support VB.NET as the corresponding VB SonarQube server plugin has been
                // updated to support the connected experience.
                // yield return VBNET;
            }
        }

        public static IEnumerable<Language> KnownLanguages
        {
            get
            {
                yield return CSharp;
                yield return VBNET;
            }
        }

        public Language(string serverKey, string name, string projectTypeGuid)
        {
            if (string.IsNullOrWhiteSpace(serverKey))
            {
                throw new ArgumentNullException(nameof(serverKey));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (string.IsNullOrWhiteSpace(projectTypeGuid))
            {
                throw new ArgumentNullException(nameof(projectTypeGuid));
            }

            this.ServerKey = serverKey;
            this.Name = name;
            this.ProjectType = new Guid(projectTypeGuid);
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
                return null;
            }

            return KnownLanguages.FirstOrDefault(x => x.ProjectType == projectKind);
        }

        #region IEquatable<Language> and Equals

        public bool Equals(Language other)
        {
            if (ReferenceEquals(other, this))
            {
                return true;
            }

            return other != null
                && other.ServerKey == this.ServerKey
                && other.ProjectType == this.ProjectType;
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as Language);
        }

        public override int GetHashCode()
        {
            return this.ServerKey.GetHashCode();
        }

        #endregion
    }
}
