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

using System.Timers;

namespace SonarLint.VisualStudio.Integration
{
    public class TimerWrapper : ITimer
    {
        private readonly Timer timer;

        public TimerWrapper()
        {
            this.timer = new Timer();
        }

        public bool AutoReset
        {
            get { return timer.AutoReset; }
            set { timer.AutoReset = value; }
        }

        public bool Enabled
        {
            get { return timer.Enabled; }
            set { timer.Enabled = value; }
        }

        public double Interval
        {
            get { return timer.Interval; }
            set { timer.Interval = value; }
        }

        public event ElapsedEventHandler Elapsed
        {
            add { timer.Elapsed += value; }
            remove { timer.Elapsed -= value; }
        }

        public void Dispose()
        {
            timer.Dispose();
        }

        public void Start() => timer.Start();

        public void Stop() => timer.Stop();
    }
}
