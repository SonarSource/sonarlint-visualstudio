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
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Infrastructure.VS;
using Task = System.Threading.Tasks.Task;

namespace SonarLint.VisualStudio.ConnectedMode.Migration.Wizard
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public sealed partial class MigrationWizardWindow : DialogWindow, IProgress<MigrationProgress>
    {
        private readonly BoundSonarQubeProject oldBinding;
        private readonly IConnectedModeMigration connectedModeMigration;
        private readonly Action onShowHelp;
        private readonly Action onShowTfvcHelp;
        private readonly Action onShowSharedBinding;
        private readonly ILogger logger;
        private readonly IThreadHandling threadHandling;

        private bool dialogResult;
        private bool migrationInProgress;

        private CancellationTokenSource cancellationTokenSource;

        internal MigrationWizardWindow(BoundSonarQubeProject oldBinding,
            IConnectedModeMigration connectedModeMigration,
            Action onShowHelp,
            Action onShowTfvcHelp, // null = don't show the Tfvc info section
            Action onShowSharedBinding,
            bool isUnderGit,
            ILogger logger)
        {
            this.oldBinding = oldBinding;
            this.connectedModeMigration = connectedModeMigration;
            this.logger = logger;
            this.onShowHelp = onShowHelp;
            this.onShowTfvcHelp = onShowTfvcHelp;
            this.onShowSharedBinding = onShowSharedBinding;
            threadHandling = ThreadHandling.Instance;

            cancellationTokenSource = new CancellationTokenSource();

            InitializeComponent();

            SetVisibilities(isUnderGit);

            this.Closing += OnClosing;
            dialogResult = false;
        }

        private void SetVisibilities(bool isUnderGit)
        {
            tfvcInfo.Visibility = isUnderGit ? Visibility.Hidden : Visibility.Visible;
            chk_SaveSharedBinding.Visibility = isUnderGit ? Visibility.Visible : Visibility.Hidden;
        }

        private void NavigateToMigrationProgressPage()
        {
            // Changing the first page visibility to Hidden rather than Collapsed so
            // that the size of the dialog does not change.
            // If we used "Collapsed", the size of the dialog would be recalculated as
            // if the first page did not exist.
            StartWindow.Visibility = Visibility.Hidden;
            MigrationProgressWindow.Visibility = Visibility.Visible;

            // Set button states
            btnPage1_Cancel.IsEnabled = false;
            btnPage1_Start.IsEnabled = false;

            finishButton.IsEnabled = false; // disabled until migration finished
        }

        private void OnStartMigration(object sender, RoutedEventArgs e)
        {
            // User has clicked on the "Start" button on the first page
            // -> show page 2
            // -> start the process

            if (migrationInProgress) { return; }
            migrationInProgress = true;

            NavigateToMigrationProgressPage();

            // Disables all closing / cancel buttons, including the
            // the X in the top-right of the window
            this.IsCloseButtonEnabled = false;
            this.btnPage2_Cancel.Focus();

            StartTimer();

            MigrateAsync().Forget();
        }

        private Timer timer;

        private void StartTimer()
        {
            panelStopwatch.Visibility = Visibility.Visible;
            timer = new Timer(OnTick, DateTimeOffset.UtcNow, 0, 300);
        }

        private void OnTick(object state)
        {
            var startTime = (DateTimeOffset)state;
            var elapsed = DateTime.UtcNow - startTime;
            var displayText = elapsed.ToString("mm\\:ss");

            threadHandling.RunOnUIThread(
                () => txtStopwatchTime.Text = displayText);
        }

        private void StopTimer() => timer.Dispose();

        private async Task MigrateAsync()
        {
            bool migrationSucceeded = false;
            try
            {
                await connectedModeMigration.MigrateAsync(oldBinding, this, cancellationTokenSource.Token);
                migrationSucceeded = true;
            }
            catch (OperationCanceledException ex)
            {
                logger.LogVerbose(MigrationStrings.CancelTokenFailure_VerboseLog, ex);
                logger.WriteLine(MigrationStrings.CancelTokenFailure_NormalLog);

                var progress = new MigrationProgress(0, 1, MigrationStrings.Wizard_Progress_Cancelled, true);
                ((IProgress<MigrationProgress>)this).Report(progress);
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.LogVerbose(MigrationStrings.ErrorDuringMigation_VerboseLog, ex);
                logger.WriteLine(MigrationStrings.ErrorDuringMigation_NormalLog, ex.Message);

                var progress = new MigrationProgress(0, 1, MigrationStrings.Wizard_Progress_Error, true);
                ((IProgress<MigrationProgress>)this).Report(progress);
            }
            finally
            {
                MigrationFinished(migrationSucceeded);
            }
        }

        private void MigrationFinished(bool result)
        {
            migrationInProgress = false;
            StopTimer();
            finishButton.IsEnabled = true;
            finishButton.Focus();
            btnPage2_Cancel.IsEnabled = false;
            IsCloseButtonEnabled = true;
            dialogResult = result;
        }

        private void OnClosing(object sender, CancelEventArgs e)
        {
            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
            migrationInProgress = false;
            this.DialogResult = dialogResult;
        }

        void IProgress<MigrationProgress>.Report(MigrationProgress value)
        {
            threadHandling.RunOnUIThread(() =>
            {
                ListBoxItem item = new ListBoxItem();
                if (value.IsWarning)
                {
                    item.Foreground = Brushes.Red;
                }
                item.Content = value.Message;
                progressList.Items.Add(item);
            });

            cancellationTokenSource.Token.ThrowIfCancellationRequested();
        }

        private void btnPage2_Cancel_Click(object sender, RoutedEventArgs e)
            => CancelMigration();

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (migrationInProgress && e.Key == Key.Escape)
            {
                CancelMigration();
            }
            base.OnKeyDown(e);
        }

        private void CancelMigration()
        {
            btnPage2_Cancel.IsEnabled = false;
            cancellationTokenSource.Cancel();
        }

        private void OnNavigateToHelp(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
            => onShowHelp?.Invoke();

        private void OnNavigateToTfvcHelp(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
            => onShowTfvcHelp?.Invoke();

        private void OnNavigateToSharedBinding(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
            => onShowSharedBinding?.Invoke();
    }
}
