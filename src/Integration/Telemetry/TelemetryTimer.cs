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

namespace SonarLint.VisualStudio.Integration
{
    public sealed class TelemetryTimer : ITelemetryTimer, IDisposable
    {
        private const double MillisecondsBeforeFirstUpload = 1000 * 60 * 5; // 5 minutes
        private const double MillisecondsBetweenUploads = 1000 * 60 * 60 * 6; // 6 hours
        private const int MinHoursBetweenUploads = 6;

        private readonly ITelemetryDataRepository telemetryRepository;
        private readonly ITimer timer;

        private bool isStarted;

        public event EventHandler<TelemetryTimerEventArgs> Elapsed;

        public TelemetryTimer(ITelemetryDataRepository telemetryRepository, ITimerFactory timerFactory)
        {
            if (telemetryRepository == null)
            {
                throw new ArgumentNullException(nameof(telemetryRepository));
            }
            if (timerFactory == null)
            {
                throw new ArgumentNullException(nameof(timerFactory));
            }

            this.telemetryRepository = telemetryRepository;

            timer = timerFactory.Create();
            timer.AutoReset = true;
            timer.Interval = MillisecondsBeforeFirstUpload;
        }

        private DateTimeOffset LastUploadDate => telemetryRepository.Data.LastUploadDate;

        public void Start()
        {
            if (!isStarted)
            {
                isStarted = true;

                timer.Elapsed += TryRaiseEvent;
                timer.Start();
            }
        }

        public void Stop()
        {
            if (isStarted)
            {
                isStarted = false;

                timer.Elapsed -= TryRaiseEvent;
                timer.Stop();
            }
        }

        private void TryRaiseEvent(object sender, TimerEventArgs e)
        {
            // After the first event we change the interval to 6h
            timer.Interval = MillisecondsBetweenUploads;

            if (e.SignalTime.IsSameDay(LastUploadDate) ||
                e.SignalTime.HoursPassedSince(LastUploadDate) < MinHoursBetweenUploads)
            {
                return;
            }

            Elapsed?.Invoke(this, new TelemetryTimerEventArgs(e.SignalTime));
        }

        public void Dispose()
        {
            Stop();

            (timer as IDisposable)?.Dispose();
        }
    }
}
