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
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.IssueVisualization.Editor;

namespace SonarLint.VisualStudio.IssueVisualization.Selection
{
    internal interface ILocationChangedEventListener : IDisposable
    {
    }

    [Export(typeof(ILocationChangedEventListener))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class LocationChangedEventListener : ILocationChangedEventListener
    {
        private readonly IDocumentNavigator documentNavigator;
        private readonly IAnalysisIssueSelectionService selectionService;
        private readonly IIssueSpanCalculator spanCalculator;
        private readonly ILogger logger;

        [ImportingConstructor]
        internal LocationChangedEventListener(IDocumentNavigator documentNavigator, 
            IAnalysisIssueSelectionService selectionService, 
            IIssueSpanCalculator spanCalculator,
            ILogger logger)
        {
            this.documentNavigator = documentNavigator;
            this.selectionService = selectionService;
            this.spanCalculator = spanCalculator;
            this.logger = logger;

            selectionService.SelectionChanged += SelectionService_SelectionChanged;
        }

        private void SelectionService_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var issueLocation = e.SelectedLocation?.Location;
            var locationFilePath = issueLocation?.FilePath;

            if (!string.IsNullOrEmpty(locationFilePath))
            {
                try
                {
                    var textView = documentNavigator.Open(locationFilePath);
                    var locationSpan = spanCalculator.CalculateSpan(issueLocation, textView.TextBuffer.CurrentSnapshot);

                    documentNavigator.Navigate(textView, locationSpan);
                }
                catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
                {
                    logger.WriteLine(Resources.ERR_OpenDocumentException, locationFilePath, ex.Message);
                }
            }
        }

        public void Dispose()
        {
            selectionService.SelectionChanged -= SelectionService_SelectionChanged;
        }
    }
}
