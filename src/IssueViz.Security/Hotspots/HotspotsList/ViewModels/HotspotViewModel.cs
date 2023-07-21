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
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Security.Hotspots.HotspotsList.ViewModels
{
    internal interface IHotspotViewModel : INotifyPropertyChanged, IDisposable
    {
        IAnalysisIssueVisualization Hotspot { get; }

        int Line { get; }

        int Column { get; }

        string DisplayPath { get; }

        string CategoryDisplayName { get; }
        
        HotspotPriority HotspotPriority { get; }
    }

    internal sealed class HotspotViewModel : IHotspotViewModel
    {
        private readonly ISecurityCategoryDisplayNameProvider categoryDisplayNameProvider;
        private readonly IIssueVizDisplayPositionCalculator positionCalculator;

        public HotspotViewModel(IAnalysisIssueVisualization hotspot, HotspotPriority hotspotPriority)
            : this(hotspot, hotspotPriority, new SecurityCategoryDisplayNameProvider(), new IssueVizDisplayPositionCalculator())
        {
        }

        internal HotspotViewModel(IAnalysisIssueVisualization hotspot, 
            HotspotPriority hotspotPriority,
            ISecurityCategoryDisplayNameProvider categoryDisplayNameProvider,
            IIssueVizDisplayPositionCalculator positionCalculator)
        {
            this.categoryDisplayNameProvider = categoryDisplayNameProvider;
            this.positionCalculator = positionCalculator;
            Hotspot = hotspot;
            HotspotPriority = hotspotPriority;
            Hotspot.PropertyChanged += Hotspot_PropertyChanged;
        }

        public IAnalysisIssueVisualization Hotspot { get; }
        
        public HotspotPriority HotspotPriority { get; }

        public int Line => positionCalculator.GetLine(Hotspot);

        public int Column => positionCalculator.GetColumn(Hotspot);

        public string DisplayPath =>
            Path.GetFileName(Hotspot.CurrentFilePath ?? ((IHotspot)Hotspot.Issue).ServerFilePath);

        public string CategoryDisplayName =>
            categoryDisplayNameProvider.Get(((IHotspot) Hotspot.Issue).Rule.SecurityCategory);

        private void Hotspot_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IAnalysisIssueVisualization.Span))
            {
                NotifyPropertyChanged(nameof(Line));
                NotifyPropertyChanged(nameof(Column));
            }
            else if (e.PropertyName == nameof(IAnalysisIssueVisualization.CurrentFilePath))
            {
                NotifyPropertyChanged(nameof(DisplayPath));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            Hotspot.PropertyChanged -= Hotspot_PropertyChanged;
        }
    }
}
