//-----------------------------------------------------------------------
// <copyright file="SonarAnalyzerCollisionManager.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using SonarLint.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    internal class SonarAnalyzerCollisionManager
    {
        private readonly Workspace workspace;

        private static readonly AssemblyName AnalyzerAssemblyName =
            new AssemblyName(typeof(SonarAnalysisContext).Assembly.FullName);
        internal /*for testing purposes*/ static readonly Version AnalyzerVersion = AnalyzerAssemblyName.Version;
        internal /*for testing purposes*/ static readonly string AnalyzerName = AnalyzerAssemblyName.Name;

        public SonarAnalyzerCollisionManager(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            var componentModel = (IComponentModel)serviceProvider.GetService(typeof(SComponentModel));
            workspace = componentModel.GetService<VisualStudioWorkspace>();

            SonarAnalysisContext.ShouldAnalysisBeDisabled =
                tree => ShouldAnalysisBeDisabledOnTree(tree);
        }

        private bool ShouldAnalysisBeDisabledOnTree(SyntaxTree tree)
        {
            if (tree == null)
            {
                return false;
            }

            IEnumerable<AnalyzerReference> references = workspace?.CurrentSolution?.GetDocument(tree)?.Project?.AnalyzerReferences;
            return HasConflictingAnalyzerReference(references);
        }

        internal /*for testing purposes*/ static bool HasConflictingAnalyzerReference(IEnumerable<AnalyzerReference> references)
        {
            if (references == null)
            {
                return false;
            }

            var sameNamedAnalyzers = references
                .Where(reference => string.Equals(reference.Display, AnalyzerName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!sameNamedAnalyzers.Any())
            {
                return false;
            }

            return sameNamedAnalyzers
                .Select(reference => (reference.Id as AssemblyIdentity)?.Version)
                .All(version => version != AnalyzerVersion);
        }
    }
}