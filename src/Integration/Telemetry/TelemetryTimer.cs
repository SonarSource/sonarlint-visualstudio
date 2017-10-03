using System;
using System.Timers;

namespace SonarLint.VisualStudio.Integration
{
    public interface ITelemetryTimer : IDisposable
    {
        event EventHandler<TelemetryTimerEventArgs> Elapsed;

        void Start();
        void Stop();
    }

    public sealed class TelemetryTimerEventArgs : EventArgs
    {
        public TelemetryTimerEventArgs(DateTime signalTime)
        {
            SignalTime = signalTime;
        }

        public DateTime SignalTime { get; }
    }

    public sealed class TelemetryTimer : ITelemetryTimer
    {
        private const double MillisecondsBeforeFirstUpload = 1000 * 60 * 1; // 1 minutes
        private const double MillisecondsBetweenUploads = 1000 * 60 * 60 * 6; // 6 hours
        private const int MinHoursBetweenUploads = 6;

        private readonly ITelemetryDataRepository telemetryRepository;
        private readonly IClock clock;
        private readonly ITimer timer;

        private bool isStarted;

        public event EventHandler<TelemetryTimerEventArgs> Elapsed;

        public TelemetryTimer(ITelemetryDataRepository telemetryRepository, IClock clock, ITimer timer)
        {
            if (telemetryRepository == null)
            {
                throw new ArgumentNullException(nameof(telemetryRepository));
            }
            if (clock == null)
            {
                throw new ArgumentNullException(nameof(clock));
            }
            if (timer == null)
            {
                throw new ArgumentNullException(nameof(timer));
            }

            this.telemetryRepository = telemetryRepository;
            this.clock = clock;

            this.timer = timer;
            timer.AutoReset = true;
            timer.Interval = MillisecondsBeforeFirstUpload;
        }

        public bool MinHoursBetweenUploadsPassed =>
            Now.Subtract(LastUploadDate).TotalHours >= MinHoursBetweenUploads;

        public bool DayChanged =>
            Now.Date.Subtract(LastUploadDate.Date).TotalDays >= 1;

        private DateTime LastUploadDate => telemetryRepository.Data.LastUploadDate;

        private DateTime Now => clock.Now;

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

        private void TryRaiseEvent(object sender, ElapsedEventArgs e)
        {
            // After the first event we change the interval to 6h
            timer.Interval = MillisecondsBetweenUploads;

            if (DayChanged && MinHoursBetweenUploadsPassed)
            {
                Elapsed?.Invoke(this, new TelemetryTimerEventArgs(signalTime: Now));
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
