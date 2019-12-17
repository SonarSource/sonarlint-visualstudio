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

using System.Collections.Generic;
using System.Threading;
using EnvDTE;
using SonarQube.Client.Models;
using Language = SonarLint.VisualStudio.Core.Language;

namespace SonarLint.VisualStudio.Integration.Binding
{
    /// <summary>
    /// Encapsulates solution-level binding operations.
    /// </summary>
    /// <remarks>
    /// * writes the binding info files and shared rulesets to disk
    /// * co-ordinates writing project-level changes (delegating to to <see cref="ProjectBindingOperation"/>)
    /// For legacy connected mode, the solution-level items are added to the solution file
    /// </remarks>
    public interface ISolutionBindingOperation : ISolutionBindingConfigFileStore
    {
        void Initialize(IEnumerable<Project> projects, IDictionary<Language, SonarQubeQualityProfile> profilesMap);

        void Prepare(CancellationToken token);

        bool CommitSolutionBinding();
    }
}
