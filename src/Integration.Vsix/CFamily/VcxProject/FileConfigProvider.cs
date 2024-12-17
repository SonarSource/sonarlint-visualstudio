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
using System.ComponentModel.Composition;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.CFamily.Analysis;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.Integration.Helpers;
using System.IO.Abstractions;

namespace SonarLint.VisualStudio.Integration.Vsix.CFamily.VcxProject
{
    internal interface IFileConfigProvider
    {
        IFileConfig Get(string analyzedFilePath, CFamilyAnalyzerOptions analyzerOptions);
    }

    [Export(typeof(IFileConfigProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    [method: ImportingConstructor]
    internal class FileConfigProvider(
        IVsUIServiceOperation uiServiceOperation,
        IFileInSolutionIndicator fileInSolutionIndicator,
        ILogger logger,
        IThreadHandling threadHandling) : IFileConfigProvider
    {
        private static readonly NoOpLogger noOpLogger = new NoOpLogger();
        public IFileSystem fileSystem { get; set; } = new FileSystem();

        public IFileConfig Get(string analyzedFilePath, CFamilyAnalyzerOptions analyzerOptions)
        {
            var analysisLogger = GetAnalysisLogger(analyzerOptions);

            return uiServiceOperation.Execute<SDTE, DTE2, IFileConfig>(dte =>
                GetInternal(analyzedFilePath, dte, analysisLogger));
        }

        private FileConfig GetInternal(string analyzedFilePath, DTE2 dte, ILogger analysisLogger)
        {
            threadHandling.ThrowIfNotOnUIThread();

            try
            {
                var projectItem = dte.Solution.FindProjectItem(analyzedFilePath);

                if (projectItem == null)
                {
                    return null;
                }

                if (!fileInSolutionIndicator.IsFileInSolution(projectItem))
                {
                    analysisLogger.LogVerbose($"[VCX:FileConfigProvider] The file is not part of a VCX project. File: {analyzedFilePath}");
                    return null;
                }
                // Note: if the C++ tools are not installed then it's likely an exception will be thrown when
                // the framework tries to JIT-compile the TryGet method (since it won't be able to find the MS.VS.VCProjectEngine
                // types).
                return FileConfig.TryGet(analysisLogger, projectItem, analyzedFilePath, fileSystem);
            }
            catch (Exception ex) when (!Microsoft.VisualStudio.ErrorHandler.IsCriticalException(ex))
            {
                analysisLogger.WriteLine(CFamilyStrings.ERROR_CreatingConfig, analyzedFilePath, ex);
                return null;
            }
        }

        private ILogger GetAnalysisLogger(CFamilyAnalyzerOptions analyzerOptions)
        {
            if (analyzerOptions is CFamilyAnalyzerOptions cFamilyAnalyzerOptions &&
                cFamilyAnalyzerOptions.CreatePreCompiledHeaders)
            {
                // In case the requeset is coming from PCH generation, we don't log failures.
                // This is to avoid redundant messages while navigating unsupported files.
                return noOpLogger;
            }

            return logger;
        }


        private class NoOpLogger : ILogger
        {
            public void WriteLine(string message)
            {
                // no-op
            }

            public void WriteLine(string messageFormat, params object[] args)
            {
                // no-op
            }

            public void LogVerbose(string message, params object[] args)
            {
                // no-op
            }
        }
    }
}
