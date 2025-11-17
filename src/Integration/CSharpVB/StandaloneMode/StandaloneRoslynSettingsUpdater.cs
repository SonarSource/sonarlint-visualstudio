/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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

using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using SonarLint.VisualStudio.Core.UserRuleSettings;

namespace SonarLint.VisualStudio.Integration.CSharpVB.StandaloneMode;

public interface IStandaloneRoslynSettingsUpdater
{
    void Update(UserSettings settings);
}

[Export(typeof(IStandaloneRoslynSettingsUpdater))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
[ExcludeFromCodeCoverage] // todo https://sonarsource.atlassian.net/browse/SLVS-2420
internal class StandaloneRoslynSettingsUpdater()
    : IStandaloneRoslynSettingsUpdater
{
    public void Update(UserSettings settings)
    {
        // TODO by https://sonarsource.atlassian.net/browse/SLVS-2420 drop this class
    }
}
