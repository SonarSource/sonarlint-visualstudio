//-----------------------------------------------------------------------
// <copyright file="PackageCommandManager.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using SonarLint.Helpers;
using System;
using System.Linq;
using System.Reflection;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    internal class SonarAnalyzerCollisionManager
    {
        private readonly IServiceProvider serviceProvider;

        private static readonly AssemblyName AnalyzerAssemblyName =
            new AssemblyName(typeof(SonarAnalysisContext).GetTypeInfo().Assembly.FullName);
        private static readonly Version AnalyzerVersion = AnalyzerAssemblyName.Version;
        private static readonly string AnalyzerName = AnalyzerAssemblyName.Name;

        public SonarAnalyzerCollisionManager(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            this.serviceProvider = serviceProvider;
        }

        internal void Initialize()
        {
            var componentModel = (IComponentModel)serviceProvider.GetService(typeof(SComponentModel));
            var workspace = componentModel.GetService<VisualStudioWorkspace>();

            SonarAnalysisContext.ShouldAnalysisBeDisabled =
                tree =>
                {
                    if (tree == null)
                    {
                        return false;
                    }

                    var references = workspace?.CurrentSolution?.GetDocument(tree)?.Project?.AnalyzerReferences;
                    if (references != null)
                    {
                        foreach (var reference in references.Where(a => a.Display == AnalyzerName))
                        {
                            var version = (reference.Id as Microsoft.CodeAnalysis.AssemblyIdentity)?.Version;
                            return version != AnalyzerVersion;
                        }
                    }

                    return false;
                };
        }
    }
}