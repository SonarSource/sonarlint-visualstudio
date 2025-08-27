/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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
using System.Reflection;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Configuration;

[Export(typeof(IRoslynAnalyzerLoader))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal class RoslynAnalyzerLoader(ILogger logger) : IRoslynAnalyzerLoader
{
    private readonly ILogger logger = logger.ForContext(Resources.RoslynAnalysisLogContext, Resources.RoslynAnalysisAnalyzerLoaderLogContext);

    [ExcludeFromCodeCoverage]
    public LoadedAnalyzerClasses LoadAnalyzerAssembly(string filePath)
    {
        try
        {
            var analyzers = new List<DiagnosticAnalyzer>();
            var codeFixProviders = new List<CodeFixProvider>();

            foreach (var type in Assembly.LoadFrom(filePath).GetTypes().Where(t => t is { IsAbstract: false, IsInterface: false, IsGenericType: false }))
            {
                if (TryLoadType(type, out CodeFixProvider? codeFixProvider))
                {
                    codeFixProviders.Add(codeFixProvider);
                }
                else if (TryLoadType(type, out DiagnosticAnalyzer? analyzer))
                {
                    analyzers.Add(analyzer);
                }
            }

            return new LoadedAnalyzerClasses(analyzers, codeFixProviders);
        }
        catch (Exception e)
        {
            logger.WriteLine(Resources.RoslynAnalysisAnalyzerLoaderFailedToLoad, filePath, e);
            return new LoadedAnalyzerClasses([], []);
        }
    }

    private bool TryLoadType<T>(Type type, [NotNullWhen(true)] out T? value) where T : class
    {
        value = null;
        try
        {
            if (typeof(T).IsAssignableFrom(type))
            {
                value = (T)Activator.CreateInstance(type);
                return true;
            }
        }
        catch (Exception e)
        {
            logger.LogVerbose(Resources.RoslynAnalysisAnalyzerLoaderFailedToLoad, type, e);
        }
        return false;
    }
}
