/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.Integration.Vsix.Analysis
{
    [Export(typeof(IAnalysisRequester))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class AnalysisRequester : IAnalysisRequester
    {
        private readonly ILogger logger;

        [ImportingConstructor]
        public AnalysisRequester(ILogger logger)
        {
            this.logger = logger;
        }

        #region IAnalysisRequester implementation

        public event EventHandler<AnalysisRequestEventArgs> AnalysisRequested;

        public void RequestAnalysis(IAnalyzerOptions analyzerOptions, params string[] filePaths)
        {
            try
            {
                var args = new AnalysisRequestEventArgs(analyzerOptions, filePaths);
                AnalysisRequested?.Invoke(this, args);
            }
            catch(Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine(Analysis.AnalysisStrings.Requester_Error, ex);
            }
        }

        #endregion IAnalysisRequester implementation
    }
}
