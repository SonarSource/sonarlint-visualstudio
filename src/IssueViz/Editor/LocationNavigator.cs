﻿/*
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
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Editor
{
    public enum NavigationResult
    {
        Failed,
        OpenedFile,
        OpenedIssue
    }
    
    public interface ILocationNavigator
    {
        NavigationResult TryNavigatePartial(IAnalysisIssueLocationVisualization locationVisualization);
        bool TryNavigate(IAnalysisIssueLocationVisualization locationVisualization);
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

        public NavigationResult TryNavigatePartial(IAnalysisIssueLocationVisualization locationVisualization)
        {
            if (locationVisualization == null)
            {
                throw new ArgumentNullException(nameof(locationVisualization));
            }

            if (!TryOpenFile(locationVisualization, out var textView))
            {
                return NavigationResult.Failed;
            }

            var locationSpan = GetOrCalculateLocationSpan(locationVisualization, textView);

            if (locationSpan.IsEmpty || !TryNavigateIssueLocation(locationVisualization, textView, locationSpan))
            {
                return NavigationResult.OpenedFile;
            }

            return NavigationResult.OpenedIssue;

        }

        private bool TryNavigateIssueLocation(IAnalysisIssueLocationVisualization locationVisualization,
            ITextView textView, SnapshotSpan locationSpan)
        {
            try
            {
                documentNavigator.Navigate(textView, locationSpan);
                return true;
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine(Resources.ERR_OpenDocumentException, locationVisualization.CurrentFilePath, ex.Message);
                locationVisualization.InvalidateSpan();
                return false;
            }
        }

        private bool TryOpenFile(IAnalysisIssueLocationVisualization locationVisualization, out ITextView textView)
        {
            textView = default;
            
            try
            {
                textView = documentNavigator.Open(locationVisualization.CurrentFilePath);
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine(Resources.ERR_OpenDocumentException, locationVisualization.CurrentFilePath, ex.Message);
                locationVisualization.InvalidateSpan();
                return false;
            }

            return true;
        }

        public bool TryNavigate(IAnalysisIssueLocationVisualization locationVisualization)
        {
            return TryNavigatePartial(locationVisualization) == NavigationResult.OpenedIssue;
        }

        private SnapshotSpan GetOrCalculateLocationSpan(IAnalysisIssueLocationVisualization locationVisualization, ITextView textView)
        {
            var shouldCalculateSpan = !locationVisualization.Span.HasValue;

            if (shouldCalculateSpan)
            {
                locationVisualization.Span = spanCalculator.CalculateSpan(locationVisualization.Location.TextRange, textView.TextBuffer.CurrentSnapshot);
            }

            return locationVisualization.Span.Value;
        }
    }
}
