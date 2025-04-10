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

namespace SonarLint.VisualStudio.Core.Binding;

public record SolutionState
{
    public bool IsOpen => SolutionName != null;
    public bool IsInConnectedMode => BindingConfiguration.Mode.IsInAConnectedMode();
    public string SolutionName { get; }
    public BindingConfiguration BindingConfiguration { get; }

    public SolutionState(string solutionName, BindingConfiguration bindingConfiguration)
    {
        SolutionName = solutionName;
        BindingConfiguration = bindingConfiguration ?? throw new ArgumentNullException(nameof(bindingConfiguration));

        Debug.Assert(!(solutionName is null && !bindingConfiguration.Equals(BindingConfiguration.Standalone)));
    }

    public virtual bool Equals(SolutionState other)
    {
        if (other is null)
        {
            return false;
        }
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return SolutionName == other.SolutionName && Equals(BindingConfiguration, other.BindingConfiguration);
    }

    public override int GetHashCode() => SolutionName?.GetHashCode() ?? 0;
}
