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

using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Security.Taint.TaintList.ViewModels
{
    internal interface ITaintIssueViewModel : INotifyPropertyChanged, IDisposable
    {
        IAnalysisIssueVisualization TaintIssueViz { get; }

        int Line { get; }

        int Column { get; }

        string DisplayPath { get; }
    }

    internal sealed class TaintIssueViewModel : ITaintIssueViewModel
    {
        private readonly IIssueVizDisplayPositionCalculator positionCalculator;

        public TaintIssueViewModel(IAnalysisIssueVisualization issue)
            : this(issue, new IssueVizDisplayPositionCalculator())
        {
        }

        internal /* for testing */ TaintIssueViewModel(IAnalysisIssueVisualization issueViz,
            IIssueVizDisplayPositionCalculator positionCalculator)
        {
            TaintIssueViz = issueViz;
            TaintIssueViz.PropertyChanged += OnPropertyChanged;

            this.positionCalculator = positionCalculator;
        }

        public IAnalysisIssueVisualization TaintIssueViz { get; }

        public int Line => positionCalculator.GetLine(TaintIssueViz);

        public int Column => positionCalculator.GetColumn(TaintIssueViz);

        public string DisplayPath =>
            Path.GetFileName(TaintIssueViz.CurrentFilePath ?? ((IAnalysisIssue)TaintIssueViz.Issue).PrimaryLocation.FilePath);

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
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
            TaintIssueViz.PropertyChanged -= OnPropertyChanged;
        }
    }
}
