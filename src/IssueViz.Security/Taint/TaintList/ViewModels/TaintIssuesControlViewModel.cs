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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.Core.Helpers;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.Infrastructure.VS.DocumentEvents;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.Security.IssuesStore;
using SonarLint.VisualStudio.IssueVisualization.Security.SharedUI;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Security.Taint.TaintList.ViewModels
{
    internal interface ITaintIssuesControlViewModel : IDisposable
    {
        ICommand NavigateCommand { get; }

        ICommand ShowInBrowserCommand { get; }

        ICollectionView IssuesView { get; }
    }

    /// <summary>
    /// View model that surfaces the data in the TaintStore
    /// </summary>
    /// <remarks>Monitors the taint store for changes and updates the view model accordingly.</remarks>
    internal sealed class TaintIssuesControlViewModel : ITaintIssuesControlViewModel
    {
        private readonly IActiveDocumentTracker activeDocumentTracker;
        private readonly IShowInBrowserService showInBrowserService;
        private readonly ITaintStore store;
        private readonly object Lock = new object();
        private string activeDocumentFilePath;

        // TODO: duncanp - make collection private
        internal /* for testing */ ObservableCollection<ITaintIssueViewModel> Issues { get; } = new ObservableCollection<ITaintIssueViewModel>();

        public ICollectionView IssuesView { get; }

        public ICommand NavigateCommand { get; private set; }

        public ICommand ShowInBrowserCommand { get; private set; }

        public TaintIssuesControlViewModel(ITaintStore store,
            ILocationNavigator locationNavigator,
            IActiveDocumentTracker activeDocumentTracker,
            IActiveDocumentLocator activeDocumentLocator,
            IShowInBrowserService showInBrowserService)
        {
            AllowMultiThreadedAccessToIssuesCollection();

            activeDocumentFilePath = activeDocumentLocator.FindActiveDocument()?.FilePath;
            this.activeDocumentTracker = activeDocumentTracker;
            activeDocumentTracker.OnDocumentFocused += ActiveDocumentTracker_OnDocumentFocused;

            this.showInBrowserService = showInBrowserService;

            this.store = store;
            this.store.IssuesChanged += Store_IssuesChanged;

            UpdateIssues();

            IssuesView = CollectionViewSource.GetDefaultView(Issues);

            SetCommands(locationNavigator);
            ApplyViewFilter(ActiveDocumentFilter);
            SetDefaultSortOrder();
        }

        private void ActiveDocumentTracker_OnDocumentFocused(object sender, DocumentFocusedEventArgs e)
        {
            activeDocumentFilePath = e.TextDocument?.FilePath;
            ApplyViewFilter(ActiveDocumentFilter);
        }

        private void ApplyViewFilter(Predicate<object> filter) =>
            IssuesView.Filter = filter;

        private bool ActiveDocumentFilter(object viewModel)
        {
            var issueFilePath = ((ITaintIssueViewModel)viewModel).TaintIssueViz.CurrentFilePath;

            if (string.IsNullOrEmpty(activeDocumentFilePath) || string.IsNullOrEmpty(issueFilePath))
            {
                return false;
            }

            return PathHelper.IsMatchingPath(issueFilePath, activeDocumentFilePath);
        }

        private void SetDefaultSortOrder() =>
            IssuesView.SortDescriptions.Add(
                new SortDescription("TaintIssueViz.Issue.CreationTimestamp", ListSortDirection.Descending));

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
                parameter =>
                {
                    var selected = (ITaintIssueViewModel)parameter;
                    locationNavigator.TryNavigate(selected.TaintIssueViz);
                },
                parameter => parameter is ITaintIssueViewModel);

            ShowInBrowserCommand = new DelegateCommand(
                parameter =>
                {
                    var selected = (ITaintIssueViewModel)parameter;
                    var taintIssue = (ITaintIssue)selected.TaintIssueViz.Issue;
                    showInBrowserService.ShowIssue(taintIssue.IssueKey);
                },
                parameter => parameter is ITaintIssueViewModel);
        }

        private void UpdateIssues()
        {
            Issues.Clear();

            foreach (var issueViz in store.GetAll())
            {
                Issues.Add(new TaintIssueViewModel(issueViz));
            }
        }

        private void Store_IssuesChanged(object sender, IssuesChangedEventArgs e)
        {
            UpdateIssues();
        }

        public void Dispose()
        {
            store.IssuesChanged -= Store_IssuesChanged;
            activeDocumentTracker.OnDocumentFocused -= ActiveDocumentTracker_OnDocumentFocused;
        }
    }
}
