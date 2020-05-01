/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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

namespace SonarLint.VisualStudio.Core.Binding
{
    public sealed class BindingConfiguration : IEquatable<BindingConfiguration>
    {
        public static readonly BindingConfiguration Standalone = new BindingConfiguration(null, SonarLintMode.Standalone, null);

        public static BindingConfiguration CreateBoundConfiguration(BoundSonarQubeProject project, SonarLintMode sonarLintMode, string bindingConfigDirectory)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            return new BindingConfiguration(project, sonarLintMode);
        }

        public BindingConfiguration(BoundSonarQubeProject project, SonarLintMode mode, string bindingConfigDirectory)
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
            return Equals(obj as BindingConfiguration);
        }

        public bool Equals(BindingConfiguration other)
        {
            if (other == null)
            {
                return false;
            }

            if (ReferenceEquals(other, this))
            {
                return true;
            }

            return other.Mode == this.Mode &&
                other.Project?.Organization?.Key == this.Project?.Organization?.Key &&
                other.Project?.ProjectKey == this.Project?.ProjectKey &&
                other.Project?.ServerUri == this.Project?.ServerUri;
        }

        public override int GetHashCode()
        {
            // The only immutable field is Mode.
            // We don't really expect this type to be used a dictionary key, but we have
            // to override GetHashCode since we have overridden Equals
            return this.Mode.GetHashCode();
        }

        #endregion
    }
}
