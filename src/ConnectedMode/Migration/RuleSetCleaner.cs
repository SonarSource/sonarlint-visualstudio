/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using SonarLint.VisualStudio.Integration;

namespace SonarLint.VisualStudio.ConnectedMode.Migration
{
    [Export(typeof(IProjectCleaner))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    /// <summary>
    /// Attempts to remove references to the generated ruleset from the project
    /// </summary>
    /// <remarks>NB this should only be called for C# or VB.NET projects - other project types
    /// won't have references to a generated ruleset</remarks>
    internal class RuleSetCleaner : IProjectCleaner
    {
        private readonly ILogger logger;

        [ImportingConstructor]
        public RuleSetCleaner(ILogger logger)
        {
            this.logger = logger;
        }

        public Task CleanAsync(Project project, IProgress<MigrationProgress> progress, CancellationToken token)
        {
            // TODO
            return Task.CompletedTask;
        }
    }
}
