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
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Integration;

namespace SonarLint.VisualStudio.IssueVisualization.Editor
{
    internal interface ILocationNavigator
    {
        bool TryNavigate(IAnalysisIssueLocation location);
    }

    [Export(typeof(ILocationNavigator))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class LocationNavigator : ILocationNavigator
    {
        private readonly IDocumentNavigator documentNavigator;
        private readonly IIssueSpanCalculator spanCalculator;
        private readonly ILogger logger;

        [ImportingConstructor]
        internal LocationNavigator(IDocumentNavigator documentNavigator, IIssueSpanCalculator spanCalculator, ILogger logger)
        {
            this.documentNavigator = documentNavigator;
            this.spanCalculator = spanCalculator;
            this.logger = logger;
        }

        public bool TryNavigate(IAnalysisIssueLocation location)
        {
            var locationFilePath = location?.FilePath;

            if (string.IsNullOrEmpty(locationFilePath))
            {
                return false;
            }

            try
            {
                var textView = documentNavigator.Open(locationFilePath);
                var locationSpan = spanCalculator.CalculateSpan(location, textView.TextBuffer.CurrentSnapshot);

                if (locationSpan.HasValue)
                {
                    documentNavigator.Navigate(textView, locationSpan.Value);

                    return true;
                }
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine(Resources.ERR_OpenDocumentException, locationFilePath, ex.Message);
            }

            return false;
        }
    }
}
