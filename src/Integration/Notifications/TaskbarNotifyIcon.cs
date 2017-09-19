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
using System.Drawing;
using Forms = System.Windows.Forms;

namespace SonarLint.VisualStudio.Integration.Notifications
{
    public class TaskbarNotifyIcon : INotifyIcon
    {
        private Forms.NotifyIcon notifyIcon = new Forms.NotifyIcon();

        public Forms.ToolTipIcon BalloonTipIcon
        {
            get
            {
                return notifyIcon.BalloonTipIcon;
            }
            set
            {
                notifyIcon.BalloonTipIcon = value;
            }
        }

        public string BalloonTipText
        {
            get
            {
                return notifyIcon.BalloonTipText;
            }
            set
            {
                notifyIcon.BalloonTipText = value;
            }
        }

        public string BalloonTipTitle
        {
            get
            {
                return notifyIcon.BalloonTipTitle;
            }
            set
            {
                notifyIcon.BalloonTipTitle = value;
            }
        }

        public Icon Icon
        {
            get
            {
                return notifyIcon.Icon;
            }
            set
            {
                notifyIcon.Icon = value;
            }
        }

        public string Text
        {
            get
            {
                return notifyIcon.Text;
            }
            set
            {
                notifyIcon.Text = value;
            }
        }

        public bool Visible
        {
            get
            {
                return notifyIcon.Visible;
            }
            set
            {
                notifyIcon.Visible = value;
            }
        }

        public event EventHandler BalloonTipClicked
        {
            add
            {
                notifyIcon.BalloonTipClicked += value;
            }
            remove
            {
                notifyIcon.BalloonTipClicked -= value;
            }
        }

        public event EventHandler Click
        {
            add
            {
                notifyIcon.Click += value;
            }
            remove
            {
                notifyIcon.Click -= value;
            }
        }

        public event EventHandler DoubleClick
        {
            add
            {
                notifyIcon.DoubleClick += value;
            }
            remove
            {
                notifyIcon.DoubleClick -= value;
            }
        }

        public void Dispose()
        {
            notifyIcon.Dispose();
            notifyIcon = null;
        }

        public void ShowBalloonTip(int timeout, string tipTitle, string tipText)
            => notifyIcon.ShowBalloonTip(timeout, tipTitle, tipText, Forms.ToolTipIcon.Info);
    }
}
