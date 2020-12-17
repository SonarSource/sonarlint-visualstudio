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
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.Core.Helpers;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.Infrastructure.VS.DocumentEvents;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Security.Taint.TaintList.ViewModels
{
    internal interface ITaintIssuesControlViewModel : IDisposable
    {
        // WIP: add selection handling
        // WIP: add any other necessary commands

        ICommand NavigateCommand { get; }

        ObservableCollection<ITaintIssueViewModel> Issues { get; }
    }

    /// <summary>
    /// View model that surfaces the data in the TaintStore
    /// </summary>
    /// <remarks>Monitors the taint store for changes and updates the view model accordingly.</remarks>
    internal sealed class TaintIssuesControlViewModel : ITaintIssuesControlViewModel
    {
        private readonly IActiveDocumentTracker activeDocumentTracker;
        private readonly object Lock = new object();
        private readonly INotifyCollectionChanged observableIssuesCollection;
        private string activeDocumentFilePath;

        public ObservableCollection<ITaintIssueViewModel> Issues { get; } = new ObservableCollection<ITaintIssueViewModel>();

        public ICommand NavigateCommand { get; private set; }

        public TaintIssuesControlViewModel(ITaintStore store, 
            ILocationNavigator locationNavigator,
            IActiveDocumentTracker activeDocumentTracker,
            IActiveDocumentLocator activeDocumentLocator)
        {
            AllowMultiThreadedAccessToIssuesCollection();

            activeDocumentFilePath = activeDocumentLocator.FindActiveDocument()?.FilePath;
            this.activeDocumentTracker = activeDocumentTracker;
            activeDocumentTracker.OnDocumentFocused += ActiveDocumentTracker_OnDocumentFocused;

            var allIssues = store.GetAll();
            observableIssuesCollection = allIssues;
            observableIssuesCollection.CollectionChanged += TaintStore_CollectionChanged;

            UpdateIssues(allIssues, Array.Empty<IAnalysisIssueVisualization>());
            SetCommands(locationNavigator);
            ApplyViewFilter(ActiveDocumentFilter);
        }

        private void ActiveDocumentTracker_OnDocumentFocused(object sender, DocumentFocusedEventArgs e)
        {
            activeDocumentFilePath = e.TextDocument?.FilePath;
            ApplyViewFilter(ActiveDocumentFilter);
        }

        private void ApplyViewFilter(Predicate<object> filter)
        {
            var collectionView = CollectionViewSource.GetDefaultView(Issues);
            collectionView.Filter = filter;
        }

        private bool ActiveDocumentFilter(object viewModel)
        {
            if (string.IsNullOrEmpty(activeDocumentFilePath))
            {
                return false;
            }

            var filePath = ((ITaintIssueViewModel) viewModel).TaintIssueViz.CurrentFilePath;

            return PathHelper.IsMatchingPath(filePath, activeDocumentFilePath);
        }

        /// <summary>
        /// Allow the observable collection <see cref="Issues"/> to be modified from a non-UI thread. 
        /// </summary>
        private void AllowMultiThreadedAccessToIssuesCollection()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            BindingOperations.EnableCollectionSynchronization(Issues, Lock);
        }

        private void SetCommands(ILocationNavigator locationNavigator)
        {
            NavigateCommand = new DelegateCommand(
                parameter => {
                    var selected = (ITaintIssueViewModel)parameter;
                    locationNavigator.TryNavigate(selected.TaintIssueViz);
                },
                parameter => parameter is ITaintIssueViewModel);
        }

        private void UpdateIssues(IEnumerable<IAnalysisIssueVisualization> addedIssueVizs, IEnumerable<IAnalysisIssueVisualization> removedIssueVizs)
        {
            foreach (var added in addedIssueVizs)
            {
                Issues.Add(new TaintIssueViewModel(added));
            }

            var issueVizsToRemove = Issues.Where(x => removedIssueVizs.Contains(x.TaintIssueViz)).ToArray();

            foreach (var removed in issueVizsToRemove)
            {
                Issues.Remove(removed);
            }
        }

        private void TaintStore_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var added = e.NewItems ?? Array.Empty<IAnalysisIssueVisualization>();
            var removed = e.OldItems ?? Array.Empty<IAnalysisIssueVisualization>();

            UpdateIssues(
                added.Cast<IAnalysisIssueVisualization>(),
                removed.Cast<IAnalysisIssueVisualization>());
        }

        public void Dispose()
        {
            observableIssuesCollection.CollectionChanged -= TaintStore_CollectionChanged;
            activeDocumentTracker.OnDocumentFocused -= ActiveDocumentTracker_OnDocumentFocused;
        }
    }
}
