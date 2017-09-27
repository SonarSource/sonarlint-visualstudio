/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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
using System.Timers;

namespace SonarLint.VisualStudio.Integration
{
    public sealed class TimerWrapper : ITimer, IDisposable
    {
        private readonly Timer timerInstance;
        private bool isDisposed;

        public TimerWrapper()
        {
            this.timerInstance = new Timer();
        }

        public event ElapsedEventHandler Elapsed
        {
            add
            {
                this.timerInstance.Elapsed += value;
            }

            remove
            {
                this.timerInstance.Elapsed -= value;
            }
        }

        public bool AutoReset
        {
            get
            {
                return this.timerInstance.AutoReset;
            }

            set
            {
                this.timerInstance.AutoReset = value;
            }
        }

        public double Interval
        {
            get
            {
                return this.timerInstance.Interval;
            }

            set
            {
                this.timerInstance.Interval = value;
            }
        }

        public void Start()
        {
            this.timerInstance.Start();
        }

        public void Stop()
        {
            this.timerInstance.Stop();
        }

        public void Close()
        {
            this.timerInstance.Close();
        }

        public void Dispose()
        {
            if (this.isDisposed)
            {
                return;
            }

            this.timerInstance.Dispose();
            this.isDisposed = true;
        }
    }
}