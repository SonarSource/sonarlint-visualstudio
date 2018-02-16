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
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarAnalyzer.Helpers;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    internal abstract class SonarAnalyzerWorkflowBase : IDisposable
    {
        internal /*for testing purposes*/ enum ProjectAnalyzerStatus
        {
            NoAnalyzer,
            SameVersion,
            DifferentVersion
        }

        private static readonly AssemblyName AnalyzerAssemblyName =
            new AssemblyName(typeof(SonarAnalysisContext).Assembly.FullName);
        internal /*for testing purposes*/ static readonly Version AnalyzerVersion = AnalyzerAssemblyName.Version;
        internal /*for testing purposes*/ static readonly string AnalyzerName = AnalyzerAssemblyName.Name;

        private readonly Workspace workspace;

        protected SonarAnalyzerWorkflowBase(Workspace workspace)
        {
            if (workspace == null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }

            this.workspace = workspace;

            SonarAnalysisContext.ShouldRegisterContextAction = ShouldRegisterContextActionWithFallback;
            SonarAnalysisContext.ShouldExecuteRegisteredAction = ShouldExecuteRegisteredAction;
        }

        internal /* for testing purposes */ bool ShouldRegisterContextActionWithFallback(
            IEnumerable<DiagnosticDescriptor> descriptors)
        {
            // Let's disable all the utility analyzers under SonarLint
            // - Note 1:
            // There is already some logic within the analyzer to disable the execution when the output-cs/vbnet folder is not
            // found which I decided to skip here.
            // - Note 2:
            // The CPD could be useful but is also disabled at the moment
            if (descriptors.Any(d => d.Id.StartsWith("S9999-")))
            {
                Debug.Assert(descriptors.Count() == 1, "Not expecting to receive multiple utility-analyzer descriptors at once");
                return false;
            }

            return ShouldRegisterContextAction(descriptors)
                // Fallback using SonarWay
                ?? descriptors.Any(d => d.CustomTags.Contains(DiagnosticTagsHelper.SonarWayTag));
        }

        internal /* for testing purposes */ protected virtual bool? ShouldRegisterContextAction(
            IEnumerable<DiagnosticDescriptor> descriptors) => null;

        internal /* for testing purposes */ bool ShouldExecuteRegisteredAction(SyntaxTree syntaxTree) =>
            syntaxTree != null &&
            GetProjectNuGetAnalyzerStatus(syntaxTree) == ProjectAnalyzerStatus.NoAnalyzer;

        protected virtual ProjectAnalyzerStatus GetProjectNuGetAnalyzerStatus(SyntaxTree syntaxTree)
        {
            var references = this.workspace.CurrentSolution?.GetDocument(syntaxTree)?.Project?.AnalyzerReferences;

            return ProcessAnalyzerReferences(references);
        }

        internal /*for testing purposes*/ static ProjectAnalyzerStatus ProcessAnalyzerReferences(
            IEnumerable<AnalyzerReference> references)
        {
            if (references == null)
            {
                return ProjectAnalyzerStatus.NoAnalyzer;
            }

            var sameNamedAnalyzers = references
                .Where(reference => string.Equals(reference.Display, AnalyzerName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (sameNamedAnalyzers.Count == 0)
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

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}