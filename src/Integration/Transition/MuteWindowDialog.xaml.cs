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

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Navigation;
using Microsoft.VisualStudio.PlatformUI;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Integration.Transition
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    [ContentProperty(nameof(MuteWindowDialog))]
    [ExcludeFromCodeCoverage]
    public partial class MuteWindowDialog : DialogWindow
    {
        private const string SonarcloudHost = "sonarcloud.io";

        private readonly IActiveSolutionBoundTracker activeSolutionBoundTracker;
        private readonly IBrowserService browserService;
        private readonly Dictionary<RadioButton, SonarQubeIssueTransition> Transitions;
        private readonly Dictionary<Border, RadioButton> BorderRadioButtons;

        public MuteWindowDialog(IActiveSolutionBoundTracker activeSolutionBoundTracker, IBrowserService browserService, bool showAccept)
        {
            InitializeComponent();

            SetVisibility(showAccept);

            this.activeSolutionBoundTracker = activeSolutionBoundTracker;
            this.browserService = browserService;
            Transitions = InitializeTransitions();
            BorderRadioButtons = InitializeBorderRadioButtons();
        }

        private Dictionary<RadioButton, SonarQubeIssueTransition> InitializeTransitions()
        {
            return new Dictionary<RadioButton, SonarQubeIssueTransition>
            {
                { rbWontFix, SonarQubeIssueTransition.WontFix },
                { rbAccept, SonarQubeIssueTransition.Accept },
                { rbFalsePositive, SonarQubeIssueTransition.FalsePositive }
            };
        }

        private Dictionary<Border, RadioButton> InitializeBorderRadioButtons()
        {
            return new Dictionary<Border, RadioButton>
            {
                {BorderAccept, rbAccept },
                {BorderWontFix, rbWontFix },
                {BorderFalsePositive, rbFalsePositive }
            };
        }

        private void SetVisibility(bool showAccept)
        {
            BorderWontFix.Visibility = showAccept ? Visibility.Hidden : Visibility.Visible;
            BorderAccept.Visibility = showAccept ? Visibility.Visible : Visibility.Hidden;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void Submit_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        public SonarQubeIssueTransition? SelectedIssueTransition { get; private set; }

        public string Comment => txtComment.Text;

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            SelectedIssueTransition = Transitions[(RadioButton)sender];
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            BorderRadioButtons[(Border)sender].IsChecked = true;
        }

        private void FormattingHelpHyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            var serverUri = activeSolutionBoundTracker.CurrentConfiguration.Project.ServerUri.ToString();
            var isSonarCloud = serverUri.Contains(SonarcloudHost);

            browserService.Navigate(isSonarCloud ? $"{serverUri}markdown/help" : $"{serverUri}formatting/help");
            e.Handled = true;
        }
    }
}
