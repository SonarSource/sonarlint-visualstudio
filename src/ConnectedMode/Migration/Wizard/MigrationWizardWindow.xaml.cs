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
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Threading;
using Task = System.Threading.Tasks.Task;

namespace SonarLint.VisualStudio.ConnectedMode.Migration.Wizard
{
    public sealed partial class MigrationWizardWindow : DialogWindow, IProgress<MigrationProgress>
    {
        public event EventHandler StartMigration;

        private bool dialogResult;

        private readonly IConnectedModeMigration connectedModeMigration;

        private bool migrationInProgress;

        internal MigrationWizardWindow(IConnectedModeMigration connectedModeMigration)
        {
            this.connectedModeMigration = connectedModeMigration;

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
            if (migrationInProgress) { return; }
            migrationInProgress = true;

            // Disables all closing / cancel buttons.
            this.migrateButton.Visibility = Visibility.Collapsed;
            this.IsCloseButtonEnabled = false;

            MigrateAsync().Forget();
        }

        private async Task MigrateAsync()
        {
            await connectedModeMigration.MigrateAsync(this, CancellationToken.None);
            MigrationFinished();
        }

        private void MigrationFinished()
        {
            this.finishButton.Visibility = Visibility.Visible;
            this.IsCloseButtonEnabled = true;
            dialogResult = true;
        }

        private void OnClosing(object sender, CancelEventArgs e)
        {
            migrationInProgress = false;
            this.DialogResult = dialogResult;
        }

        void IProgress<MigrationProgress>.Report(MigrationProgress value)
        {
            ListBoxItem item = new ListBoxItem();
            item.Foreground = value.IsWarning ? Brushes.Red : Brushes.Black;
            item.Content = value.Message;
            progressList.Items.Add(item);
        }
    }
}
