using System.IO;
using SonarLint.VisualStudio.Core.Binding;

namespace SonarLint.VisualStudio.ConnectedMode.Binding
{
    [Obsolete("Use BindingConfiguration instead. This class is kept for backwards compatibility with binding formats that don't support ServerConnection as a separate entity")]
    internal sealed class LegacyBindingConfiguration : IEquatable<LegacyBindingConfiguration>
    {
        public static readonly LegacyBindingConfiguration Standalone = new LegacyBindingConfiguration(null, SonarLintMode.Standalone, null);

        public static LegacyBindingConfiguration CreateBoundConfiguration(BoundSonarQubeProject project, SonarLintMode sonarLintMode, string bindingConfigDirectory)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            if (string.IsNullOrEmpty(bindingConfigDirectory))
            {
                throw new ArgumentNullException(nameof(bindingConfigDirectory));
            }

            return new LegacyBindingConfiguration(project, sonarLintMode, bindingConfigDirectory);
        }

        public LegacyBindingConfiguration(BoundSonarQubeProject project, SonarLintMode mode, string bindingConfigDirectory)
        {
            Project = project;
            Mode = mode;
            BindingConfigDirectory = bindingConfigDirectory;
        }

        public BoundSonarQubeProject Project { get; }

        public SonarLintMode Mode { get; }

        public string BindingConfigDirectory { get; }

        #region IEquatable<BindingConfiguration> and Equals

        public override bool Equals(object obj)
        {
            return Equals(obj as LegacyBindingConfiguration);
        }

        public bool Equals(LegacyBindingConfiguration other)
        {
            if (other == null)
            {
                return false;
            }

            if (ReferenceEquals(other, this))
            {
                return true;
            }

            return other.Mode == Mode &&
                   other.Project?.Organization?.Key == Project?.Organization?.Key &&
                   other.Project?.ProjectKey == Project?.ProjectKey &&
                   other.Project?.ServerUri == Project?.ServerUri;
        }

        public override int GetHashCode()
        {
            // The only immutable field is Mode.
            // We don't really expect this type to be used a dictionary key, but we have
            // to override GetHashCode since we have overridden Equals
            return Mode.GetHashCode();
        }

        #endregion

        public string BuildPathUnderConfigDirectory(string fileSuffix = "")
        {
            var escapedFileName = Core.Helpers.PathHelper.EscapeFileName(fileSuffix).ToLowerInvariant(); // Must be lower case - see https://github.com/SonarSource/sonarlint-visualstudio/issues/1068;

            return Path.Combine(BindingConfigDirectory, escapedFileName);
        }
    }
}
