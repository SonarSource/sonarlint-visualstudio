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
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration;
using Task = System.Threading.Tasks.Task;

namespace SonarLint.VisualStudio.ConnectedMode.Migration.Wizard
{
    public sealed partial class MigrationWizardWindow : DialogWindow, IProgress<MigrationProgress>
    {
        public event EventHandler StartMigration;

        private readonly IConnectedModeMigration connectedModeMigration;
        private readonly ILogger logger;

        private bool dialogResult;
        private bool migrationInProgress;

        private CancellationTokenSource cancellationTokenSource;

        internal MigrationWizardWindow(IConnectedModeMigration connectedModeMigration, ILogger logger)
        {
            this.connectedModeMigration = connectedModeMigration;
            this.logger = logger;

            cancellationTokenSource = new CancellationTokenSource();

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

            this.migrateButton.Visibility = Visibility.Collapsed;
            // Disables all closing / cancel buttons.
            this.IsCloseButtonEnabled = false;

            MigrateAsync().Forget();
        }

        private async Task MigrateAsync()
        {
            try
            {
                await connectedModeMigration.MigrateAsync(this, cancellationTokenSource.Token);
                MigrationFinished();
            }
            catch (OperationCanceledException ex)
            {
                logger.LogVerbose(MigrationStrings.CancelTokenFailure_VerboseLog, ex);
                logger.WriteLine(MigrationStrings.CancelTokenFailure_NormalLog);
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.LogVerbose(MigrationStrings.ErrorDuringMigation_VerboseLog, ex);
                logger.WriteLine(MigrationStrings.ErrorDuringMigation_NormalLog, ex.Message);
            }
        }

        private void MigrationFinished()
        {
            migrationInProgress = false;
            this.finishButton.Visibility = Visibility.Visible;
            this.IsCloseButtonEnabled = true;
            dialogResult = true;
        }

        private void OnClosing(object sender, CancelEventArgs e)
        {
            cancellationTokenSource.Cancel();
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
