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
using System.Windows;
using Microsoft.VisualStudio.PlatformUI;
using System.ComponentModel;

namespace SonarLint.VisualStudio.ConnectedMode.Migration.Wizard
{
    public sealed partial class MigrationWizardWindow : DialogWindow
    {
        public event EventHandler StartMigration;

        private bool dialogResult;

        internal MigrationWizardWindow()
        {
            InitializeComponent();
            this.Closing += OnClosing;
            dialogResult = false;
        }

        private void NavigateToMigrationProgressPage(object sender, RoutedEventArgs e)
        {
            StartWindow.Visibility = Visibility.Collapsed;
            MigrationProgressWindow.Visibility = Visibility.Visible;
        }

        private void OnStartMigration(object sender, RoutedEventArgs e)
        {
            // Disables all closing / cancel buttons.
            this.migrateButton.Visibility = Visibility.Collapsed;
            this.IsCloseButtonEnabled = false;
            MigrationFinished();
        }

        private void MigrationFinished()
        {
            this.finishButton.Visibility = Visibility.Visible;
            this.IsCloseButtonEnabled = true;
            dialogResult = true;
        }

        private void OnClosing(object sender, CancelEventArgs e) => this.DialogResult = dialogResult;
    }
}
