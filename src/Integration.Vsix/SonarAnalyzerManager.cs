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
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarAnalyzer.Helpers;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    internal sealed class SonarAnalyzerManager
    {
        internal /*for testing purposes*/ enum ProjectAnalyzerStatus
        {
            NoAnalyzer,
            SameVersion,
            DifferentVersion
        }

        private readonly Workspace workspace;
        private readonly IActiveSolutionBoundTracker activeSolutionBoundTracker;

        private static readonly AssemblyName AnalyzerAssemblyName =
            new AssemblyName(typeof(SonarAnalysisContext).Assembly.FullName);
        internal /*for testing purposes*/ static readonly Version AnalyzerVersion = AnalyzerAssemblyName.Version;
        internal /*for testing purposes*/ static readonly string AnalyzerName = AnalyzerAssemblyName.Name;

        internal /*for testing purposes*/ SonarAnalyzerManager(IActiveSolutionBoundTracker activeSolutionBoundTracker,
            Workspace workspace)
        {
            if (activeSolutionBoundTracker == null)
            {
                throw new ArgumentNullException(nameof(activeSolutionBoundTracker));
            }

            if (workspace == null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }

            this.activeSolutionBoundTracker = activeSolutionBoundTracker;
            this.workspace = workspace;

            SonarAnalysisContext.ShouldAnalysisBeDisabled = tree => ShouldAnalysisBeDisabledOnTree(tree);
        }

        private bool ShouldAnalysisBeDisabledOnTree(SyntaxTree tree)
        {
            if (tree == null)
            {
                return false;
            }

            IEnumerable<AnalyzerReference> references = workspace?.CurrentSolution?.GetDocument(tree)?.Project?.AnalyzerReferences;
            ProjectAnalyzerStatus projectAnalyzerStatus = GetProjectAnalyzerConflictStatus(references);

            return HasConflictingAnalyzerReference(projectAnalyzerStatus) ||
                this.GetIsBoundWithoutAnalyzer(projectAnalyzerStatus);
        }

        internal /*for testing purposes*/ bool GetIsBoundWithoutAnalyzer(ProjectAnalyzerStatus projectAnalyzerStatus)
        {
            return projectAnalyzerStatus == ProjectAnalyzerStatus.NoAnalyzer &&
                this.activeSolutionBoundTracker != null &&
                this.activeSolutionBoundTracker.IsActiveSolutionBound;
        }

        internal /*for testing purposes*/ static bool HasConflictingAnalyzerReference(
            ProjectAnalyzerStatus projectAnalyzerStatus)
        {
            return projectAnalyzerStatus == ProjectAnalyzerStatus.DifferentVersion;
        }

        internal /*for testing purposes*/ static ProjectAnalyzerStatus GetProjectAnalyzerConflictStatus(
            IEnumerable<AnalyzerReference> references)
        {
            if (references == null)
            {
                return ProjectAnalyzerStatus.NoAnalyzer;
            }

            List<AnalyzerReference> sameNamedAnalyzers = references
                .Where(reference => string.Equals(reference.Display, AnalyzerName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!sameNamedAnalyzers.Any())
            {
                return ProjectAnalyzerStatus.NoAnalyzer;
            }

            bool hasConflictingAnalyzer = sameNamedAnalyzers
                .Select(reference => (reference.Id as AssemblyIdentity)?.Version)
                .All(version => version != AnalyzerVersion);

            return hasConflictingAnalyzer
                ? ProjectAnalyzerStatus.DifferentVersion
                : ProjectAnalyzerStatus.SameVersion;
        }
    }
}