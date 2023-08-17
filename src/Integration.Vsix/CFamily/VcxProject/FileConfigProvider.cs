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
using EnvDTE;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.CFamily.Analysis;
using SonarLint.VisualStudio.Integration.Helpers;

namespace SonarLint.VisualStudio.Integration.Vsix.CFamily.VcxProject
{
    internal interface IFileConfigProvider
    {
        IFileConfig Get(ProjectItem projectItem, string analyzedFilePath, CFamilyAnalyzerOptions analyzerOptions);
    }

    internal class FileConfigProvider : IFileConfigProvider
    {
        private static readonly NoOpLogger noOpLogger = new NoOpLogger();
        private readonly ILogger logger;

        public FileConfigProvider(ILogger logger)
        {
            this.logger = logger;
        }


        public IFileConfig Get(ProjectItem projectItem, string analyzedFilePath, CFamilyAnalyzerOptions analyzerOptions)
        {
            var analysisLogger = GetAnalysisLogger(analyzerOptions);

            if (!IsFileInSolution(projectItem))
            {
                analysisLogger.LogVerbose($"[VCX:FileConfigProvider] The file is not part of a VCX project. File: {analyzedFilePath}");
                return null;
            }

            try
            {
                // Note: if the C++ tools are not installed then it's likely an exception will be thrown when
                // the framework tries to JIT-compile the TryGet method (since it won't be able to find the MS.VS.VCProjectEngine
                // types).
                return FileConfig.TryGet(analysisLogger, projectItem, analyzedFilePath);
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

        internal static bool IsFileInSolution(ProjectItem projectItem)
        {
            try
            {
                // Issue 667:  https://github.com/SonarSource/sonarlint-visualstudio/issues/667
                // If you open a C++ file that is not part of the current solution then
                // VS will cruft up a temporary vcxproj so that it can provide language
                // services for the file (e.g. syntax highlighting). This means that
                // even though we have what looks like a valid project item, it might
                // not actually belong to a real project.
                var indexOfSingleFileString = projectItem?.ContainingProject?.FullName.IndexOf("SingleFileISense", StringComparison.OrdinalIgnoreCase);
                return indexOfSingleFileString.HasValue &&
                       indexOfSingleFileString.Value <= 0 &&
                       projectItem.ConfigurationManager != null &&
                       // the next line will throw if the file is not part of a solution
                       projectItem.ConfigurationManager.ActiveConfiguration != null;
            }
            catch (Exception ex) when (!Microsoft.VisualStudio.ErrorHandler.IsCriticalException(ex))
            {
                // Suppress non-critical exceptions
            }
            return false;
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
