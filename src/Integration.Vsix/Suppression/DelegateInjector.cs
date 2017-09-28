/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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
using System.Reflection;
using Microsoft.CodeAnalysis;

/* We want to suppress issues from Sonar analyzers that are added via NuGet.
 * If the analyzer in the NuGet is of a different version from the one in the VSIX then just
 * setting the static property on the version referenced by the VSIX won't work (both analyzer
 * assemblies will be loaded in memory, each with its own static class + property).
 * We need to set the static property for each Sonar analyzer assembly that is loaded.
 * 
 * Version-compatibility: the NuGet package and VSIX might reference different versions of Roslyn etc,git branch
 * but we need to be able to assign a delegate from the VSIX to the NuGet static property.
 * If the NuGet package is using a higher version of Roslyn then the analyzer won't work anyway.
 * If is is using a lower version of Roslyn then the binding redirect in VS should make the assignment works.
 */

namespace SonarLint.VisualStudio.Integration.Vsix.Suppression
{
    /// <summary>
    /// Injects the suppression calculator function into any versions of the Sonar
    /// analyzers that are already loaded, or in to any new versions as they are loaded
    /// </summary>
    internal sealed class DelegateInjector : IDisposable
    {
        private readonly Func<Diagnostic, bool> suppressionFunction;
        private readonly IServiceProvider serviceProvider;

        public DelegateInjector(Func<Diagnostic, bool> suppressionFunction, IServiceProvider serviceProvider)
        {
            if (suppressionFunction == null)
            {
                throw new ArgumentNullException(nameof(suppressionFunction));
            }
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            this.suppressionFunction = suppressionFunction;
            this.serviceProvider = serviceProvider;
            // Inject the delegate into any Sonar analyzer assemblies that are already loaded
            foreach(Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                InjectSuppressionDelegate(asm);
            }

            // Monitor assemblies as they are loaded and inject the delegate if necessary
            AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;
        }

        private void OnAssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            InjectSuppressionDelegate(args.LoadedAssembly);
        }

        private void InjectSuppressionDelegate(Assembly asm)
        {
            // If this is a Sonar analyzer assembly, try to set the suppression delegate
            // Note: the property might not exist for down-level versions of the analyzer
            const string AssemblyName = "SonarAnalyzer";

            if (asm.FullName.StartsWith(AssemblyName, StringComparison.OrdinalIgnoreCase))
            {
                SafeSetProperty(asm);
            }
        }

        private void SafeSetProperty(Assembly asm)
        {
            const string FullTypeName = "SonarAnalyzer.Helpers.SonarAnalysisContext";
            const string PropertyName = "ShouldDiagnosticBeReported";

            try
            {
                Type baseType = asm.GetType(FullTypeName, throwOnError: false);

                PropertyInfo pi = baseType?.GetProperty(PropertyName, BindingFlags.Public | BindingFlags.Static);

                pi?.SetValue(null, this.suppressionFunction);
            }
            catch (Exception e)
            {
                // Suppress failures - we don't want the analyzers to fail
                VsShellUtils.WriteToSonarLintOutputPane(serviceProvider,
                    $@"Unable to set the analyzer suppression handler for {asm.FullName}.
SonarQube issues that have been suppressed in SonarQube may still be reported in the IDE.
    Assembly location: {asm.Location}
    Error: {e.Message}");
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    AppDomain.CurrentDomain.AssemblyLoad -= OnAssemblyLoad;
                }

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