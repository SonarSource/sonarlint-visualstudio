﻿/*
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
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using SonarAnalyzer.Helpers;
using SonarLint.VisualStudio.Integration.Vsix.Suppression;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    internal class SonarAnalyzerLegacyConnectedWorkflow : SonarAnalyzerWorkflowBase
    {
        private readonly ISuppressionHandler suppressionHandler;
        private readonly ILogger logger;

        private readonly Func<SyntaxTree, Diagnostic, bool> shouldDiagnosticBeReportedFunc;

        public SonarAnalyzerLegacyConnectedWorkflow(Workspace workspace, ISuppressionHandler suppressionHandler, ILogger logger)
            : base(workspace)
        {
            if (suppressionHandler == null)
            {
                throw new ArgumentNullException(nameof(suppressionHandler));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            this.logger = logger;
            this.shouldDiagnosticBeReportedFunc = ShouldIssueBeReported;
            this.suppressionHandler = suppressionHandler;

            SonarAnalysisContext.ShouldExecuteRuleFunc = ShouldExecuteVsixAnalyzer;

            // Inject the delegate into any Sonar analyzer assemblies that are already loaded
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                InjectSuppressionDelegate(asm);
            }

            // Monitor assemblies as they are loaded and inject the delegate if necessary
            AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;
        }

        internal /* for testing purposes */ bool ShouldExecuteVsixAnalyzer(IAnalysisRunContext context)
        {
            if (context.SyntaxTree == null)
            {
                return false;
            }

            Debug.Assert(context.SupportedDiagnostics?.Any() ?? false,
                "Not expecting a null or empty collection of diagnostic descriptors");

            // Disable the VSIX analyzer as we want the NuGet analyzer to take precedence
            if (GetProjectNuGetAnalyzerStatus(context.SyntaxTree) != ProjectAnalyzerStatus.NoAnalyzer)
            {
                return false;
            }

            // If the project doesn't have any NuGet we still want to provide the SonarWay level of analysis
            return context.SupportedDiagnostics.Any(d => d.CustomTags.Contains(DiagnosticTagsHelper.SonarWayTag));
        }

        private bool ShouldIssueBeReported(SyntaxTree syntaxTree, Diagnostic diagnostic) =>
            this.suppressionHandler.ShouldIssueBeReported(syntaxTree, diagnostic);

        private void OnAssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            InjectSuppressionDelegate(args.LoadedAssembly);
        }

        private void InjectSuppressionDelegate(Assembly asm)
        {
            // If this is a Sonar analyzer assembly, try to set the suppression delegate
            // Note: the property might not exist for down-level versions of the analyzer
            if (asm.FullName.StartsWith(AnalyzerName, StringComparison.OrdinalIgnoreCase))
            {
                SafeSetProperty(asm);
            }
        }

        private void SafeSetProperty(Assembly asm)
        {
            const string FullTypeName = "SonarAnalyzer.Helpers.SonarAnalysisContext";
            // We still need to set the obsolete delegate for compatibility reasons
            // (it will be set on all loaded analyzers and NuGet ones can be older).
            const string PropertyName = nameof(SonarAnalysisContext.ShouldDiagnosticBeReported);

            try
            {
                var baseType = asm.GetType(FullTypeName, throwOnError: false);
                var propertyInfo = baseType?.GetProperty(PropertyName, BindingFlags.Public | BindingFlags.Static);

                propertyInfo?.SetValue(null, this.shouldDiagnosticBeReportedFunc);
            }
            catch (Exception e)
            {
                // Suppress failures - we don't want the analyzers to fail
                this.logger.WriteLine($@"Unable to set the analyzer suppression handler for {asm.FullName}.
SonarQube issues that have been suppressed in SonarQube may still be reported in the IDE.
    Assembly location: {asm.Location}
    Error: {e.Message}");
            }
        }

        #region IDisposable Support
        private bool disposedValue = false;
        protected override void Dispose(bool disposing)
        {
            if (disposedValue)
            {
                return;
            }

            base.Dispose(disposing);
            AppDomain.CurrentDomain.AssemblyLoad -= OnAssemblyLoad;
            disposedValue = true;
        }
        #endregion
    }
}