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
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Security.SelectionService
{
    internal interface IHotspotsSelectionService : IDisposable
    {
        event EventHandler<SelectionChangedEventArgs> SelectionChanged;

        void Select(IAnalysisIssueVisualization hotspot);
    }

    [Export(typeof(IHotspotsSelectionService))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class HotspotsSelectionService : IHotspotsSelectionService
    {
        public event EventHandler<SelectionChangedEventArgs> SelectionChanged;

        public void Select(IAnalysisIssueVisualization hotspot)
        {
            RaiseSelectionChanged(hotspot);
        }

        private void RaiseSelectionChanged(IAnalysisIssueVisualization analysisIssueVisualization)
        {
            SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(analysisIssueVisualization));
        }

        public void Dispose()
        {
            SelectionChanged = null;
        }
    }

    internal class SelectionChangedEventArgs : EventArgs
    {
        public SelectionChangedEventArgs(IAnalysisIssueVisualization selectedHotspot)
        {
            SelectedHotspot = selectedHotspot;
        }

        public IAnalysisIssueVisualization SelectedHotspot { get; }
    }
}
