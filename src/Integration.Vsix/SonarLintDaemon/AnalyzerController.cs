/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2019 SonarSource SA
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

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using EnvDTE;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    [Export(typeof(IAnalyzerController))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class AnalyzerController : IAnalyzerController
    {
        private readonly ILogger logger;
        private readonly IEnumerable<IAnalyzer> analyzers;

        [ImportingConstructor]
        public AnalyzerController(ILogger logger,
            [ImportMany]IEnumerable<IAnalyzer> analyzers)
        {
            this.logger = logger;
            this.analyzers = analyzers;
        }

        public bool IsAnalysisSupported(IEnumerable<AnalysisLanguage> languages)
        {
            bool isSupported = analyzers.Any(a => a.IsAnalysisSupported(languages));
            return isSupported;
        }

        public void RequestAnalysis(string path, string charset, IEnumerable<AnalysisLanguage> detectedLanguages, IIssueConsumer consumer, ProjectItem projectItem)
        {
            bool handled = false;
            foreach(var analyzer in analyzers)
            {
                if (analyzer.IsAnalysisSupported(detectedLanguages))
                {
                    handled = true;
                    analyzer.RequestAnalysis(path, charset, detectedLanguages, consumer, projectItem);
                }
            }

            if (!handled)
            {
                logger.WriteLine($"No analyzer supported analysis of {path}");
            }
        }
    }
}
