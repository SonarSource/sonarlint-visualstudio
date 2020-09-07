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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.Suppression;
using SonarLint.VisualStudio.Integration.Vsix.Analysis;
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.Integration.Vsix.SonarLintTagger
{
    internal interface IIssueTrackerFactory
    {
        IIssueTracker Create(ITextDocument textDocument, IEnumerable<AnalysisLanguage> detectedLanguages);
    }

    [Export(typeof(IIssueTrackerFactory))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class IssueTrackerFactory : IIssueTrackerFactory
    {
        private readonly IAnalysisIssueVisualizationConverter converter;
        private readonly ISonarErrorListDataSource sonarErrorDataSource;
        private readonly IIssuesFilter issuesFilter;
        private readonly IScheduler scheduler;
        private readonly IAnalyzerController analyzerController;
        private readonly ILogger logger;
        private readonly DTE dte;

        [ImportingConstructor]
        public IssueTrackerFactory([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider, 
            IAnalysisIssueVisualizationConverter converter, 
            ISonarErrorListDataSource sonarErrorDataSource, 
            IIssuesFilter issuesFilter, 
            IScheduler scheduler, 
            IAnalyzerController analyzerController, 
            ILogger logger)
        {
            this.converter = converter;
            this.sonarErrorDataSource = sonarErrorDataSource;
            this.issuesFilter = issuesFilter;
            this.scheduler = scheduler;
            this.analyzerController = analyzerController;
            this.logger = logger;
            this.dte = serviceProvider.GetService<DTE>();
        }

        public IIssueTracker Create(ITextDocument textDocument, IEnumerable<AnalysisLanguage> detectedLanguages)
        {
            return new TextBufferIssueTracker(dte, textDocument, detectedLanguages, issuesFilter, sonarErrorDataSource,
                converter, scheduler, analyzerController, logger);
        }
    }
}
