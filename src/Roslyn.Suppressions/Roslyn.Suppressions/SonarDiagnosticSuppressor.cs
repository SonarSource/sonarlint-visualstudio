/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarLint.VisualStudio.Core.ETW;

namespace SonarLint.VisualStudio.Roslyn.Suppressions
{
    /// <summary>
    /// Diagnostic suppressor that can suppress all Sonar C# and VB.NET diagnostics
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    internal class SonarDiagnosticSuppressor : DiagnosticSuppressor
    {
        /// <summary>
        /// Func is used so that we could initialize the container lazily only when we are in connected mode.
        /// </summary>
        private readonly Func<IContainer> getContainer;

        public SonarDiagnosticSuppressor () : this(() => Container.Instance)
        {

        }

        internal SonarDiagnosticSuppressor(Func<IContainer> getContainer)
        {
            this.getContainer = getContainer;
        }

        public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions => SupportedSuppressionsBuilder.Instance.Descriptors;

        public override void ReportSuppressions(SuppressionAnalysisContext context)
        {
            CodeMarkers.Instance.ReportSuppressionsStart();
            var executionContext = new SuppressionExecutionContext(context.Options);

            var suppressions = GetSuppressions(context.ReportedDiagnostics, executionContext);

            //SuppressionAnalysisContext is a public struct with an internal constructor and because of that we can't mock or create it
            //To be able the test we had to seperate parts of code that do not use SuppressionAnalysisContext directly and had to loop twice
            foreach (var suppression in suppressions)
            {
                context.ReportSuppression(suppression);
            }
            CodeMarkers.Instance.ReportSuppressionsStop(executionContext.Mode, suppressions.Count());
        }

        internal /*For Testing*/ IEnumerable<Suppression> GetSuppressions(ImmutableArray<Diagnostic> ReportedDiagnostics, ISuppressionExecutionContext executionContext)
        {
            if (!executionContext.IsInConnectedMode)
            {
                return Enumerable.Empty<Suppression>();
            }
            var result = new List<Suppression>();

            var container = getContainer();

            foreach (var diag in ReportedDiagnostics.Where(diag => container.SuppressionChecker.IsSuppressed(diag, executionContext.SettingsKey)))
            {
                var suppressionDesc = SupportedSuppressions.Single(x => x.SuppressedDiagnosticId == diag.Id);
                result.Add(Suppression.Create(suppressionDesc, diag));                
            }
            return result;
        }
    }
}
