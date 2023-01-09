﻿/*
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

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Editor.SelectedIssueTagging.Adornment
{
    partial class IssueLocationAdornmentTagger : FilteringTaggerBase<ISelectedIssueLocationTag, IntraTextAdornmentTag>
    {
        internal interface ICachingAdornmentFactory
        {
            /// <summary>
            /// Clears unnecessary entries from the cache
            /// </summary>
            /// <param name="currentLocations">The list of locations that are still in use</param>
            void Refresh(IEnumerable<IAnalysisIssueLocationVisualization> currentLocations);

            IssueLocationAdornment CreateOrUpdate(IAnalysisIssueLocationVisualization locationViz);
        }

        internal class CachingAdornmentFactory : ICachingAdornmentFactory
        {
            private readonly IWpfTextView wpfTextView;
            private readonly IDictionary<IAnalysisIssueLocationVisualization, IssueLocationAdornment> locVizToAdornmentMap;

            internal /* for testing */ IReadOnlyCollection<IssueLocationAdornment> CachedAdornments => locVizToAdornmentMap.Values.ToList();

            public CachingAdornmentFactory(IWpfTextView view)
            {
                wpfTextView = view;
                locVizToAdornmentMap = new Dictionary<IAnalysisIssueLocationVisualization, IssueLocationAdornment>();
            }

            public IssueLocationAdornment CreateOrUpdate(IAnalysisIssueLocationVisualization locationViz)
            {
                // As long as the cache is always accessed on the same thread we don't need to worry
                // about synchronising access to it. Currently VS is always running this code on the
                // main thread. The assertion is to detect if this behaviour changes.
                Debug.Assert(ThreadHelper.CheckAccess(), "Expected cache to be accessed on the UI thread");

                if (locVizToAdornmentMap.TryGetValue(locationViz, out var existingAdornment))
                {
                    existingAdornment.Update(wpfTextView.FormattedLineSource);
                    return existingAdornment;
                }

                var newAdornment = CreateAdornment(locationViz);
                locVizToAdornmentMap[locationViz] = newAdornment;
                return newAdornment;
            }

            public void Refresh(IEnumerable<IAnalysisIssueLocationVisualization> currentLocations)
            {
                Debug.Assert(ThreadHelper.CheckAccess(), "Expected cache to be accessed on the UI thread");

                var unused = locVizToAdornmentMap.Keys.Except(currentLocations).ToArray();
                foreach (var item in unused)
                {
                    locVizToAdornmentMap.Remove(item);
                }
            }

            private IssueLocationAdornment CreateAdornment(IAnalysisIssueLocationVisualization locationViz)
            {
                var adornment = new IssueLocationAdornment(locationViz, wpfTextView.FormattedLineSource);

                // If we don't call Measure here the tag is positioned incorrectly
                adornment.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
                return adornment;
            }
        }
    }
}
