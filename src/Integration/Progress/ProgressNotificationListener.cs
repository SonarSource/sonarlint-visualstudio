//-----------------------------------------------------------------------
// <copyright file="ProgressNotificationListener.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.Progress.Controller;
using System;

namespace SonarLint.VisualStudio.Integration.Progress
{
    /// <summary>
    /// The listener will forward the progress notifications to the output window.
    /// The listener will ignore empty and duplicate messages (duplicate with the previous one notification progress message)
    /// </summary>
    public class ProgressNotificationListener : IDisposable
    {
        private readonly IProgressEvents progressEvents;
        private readonly IServiceProvider serviceProvider;
        private string previousProgressDetail;

        public ProgressNotificationListener(IServiceProvider serviceProvider, IProgressEvents progressEvents)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            if (progressEvents == null)
            {
                throw new ArgumentNullException(nameof(progressEvents));
            }

            this.serviceProvider = serviceProvider;
            this.progressEvents = progressEvents;

            this.progressEvents.StepExecutionChanged += this.OnStepExecutionChanged;
        }

        public string MessageFormat
        {
            get;
            set;
        }

        private void OnStepExecutionChanged(object sender, StepExecutionChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(e.ProgressDetailText) && !StringComparer.CurrentCulture.Equals(previousProgressDetail, e.ProgressDetailText))
            {
                previousProgressDetail = e.ProgressDetailText;
                string format = string.IsNullOrWhiteSpace(this.MessageFormat) ? "{0}" : this.MessageFormat;
                VsShellUtils.WriteToSonarLintOutputPane(this.serviceProvider, format, e.ProgressDetailText);
            }
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    this.progressEvents.StepExecutionChanged -= this.OnStepExecutionChanged;
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}
