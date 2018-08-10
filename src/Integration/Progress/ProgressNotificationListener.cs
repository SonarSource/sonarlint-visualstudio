/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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
using SonarLint.VisualStudio.Progress.Controller;

namespace SonarLint.VisualStudio.Integration.Progress
{
    /// <summary>
    /// The listener will forward the progress notifications to the output window.
    /// The listener will ignore empty and duplicate messages (duplicate with the previous one notification progress message)
    /// </summary>
    public sealed class ProgressNotificationListener : IDisposable
    {
        private readonly IProgressEvents progressEvents;
        private readonly ILogger logger;
        private string previousProgressDetail;

        public ProgressNotificationListener(IProgressEvents progressEvents, ILogger logger)
        {
            if (progressEvents == null)
            {
                throw new ArgumentNullException(nameof(progressEvents));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            this.progressEvents = progressEvents;
            this.logger = logger;

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
                this.logger.WriteLine(format, e.ProgressDetailText);
            }
        }

        #region IDisposable Support
        private bool disposedValue;

        private void Dispose(bool disposing)
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
