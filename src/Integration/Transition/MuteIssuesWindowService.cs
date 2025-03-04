﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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

using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using SonarLint.VisualStudio.ConnectedMode.Transition;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Integration.Transition
{
    [Export(typeof(IMuteIssuesWindowService))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    [method: ImportingConstructor]
    internal class MuteIssuesWindowService(IActiveSolutionBoundTracker activeSolutionBoundTracker, IBrowserService browserService) : IMuteIssuesWindowService
    {
        [ExcludeFromCodeCoverage]
        public MuteIssuesWindowResponse Show(IEnumerable<SonarQubeIssueTransition> allowedTransitions)
        {
            var dialog = new MuteWindowDialog(activeSolutionBoundTracker, browserService, allowedTransitions) { Owner = Application.Current.MainWindow };
            var dialogResult = dialog.ShowDialog();

            return new MuteIssuesWindowResponse
            {
                Result = dialogResult.GetValueOrDefault(), IssueTransition = dialog.ViewModel.SelectedStatusViewModel?.Transition, Comment = dialog.ViewModel?.Comment
            };
        }
    }
}
