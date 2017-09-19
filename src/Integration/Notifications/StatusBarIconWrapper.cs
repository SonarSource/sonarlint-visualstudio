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
using System.Windows;
using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.Integration.WPF;

namespace SonarLint.VisualStudio.Integration.Notifications
{
    public class StatusBarIconWrapper : ViewModelBase, INotifyIcon
    {
        private readonly NotificationIndicator notificationIndicator;
        private string balloonTipText;
        private string text;
        private bool hasEvents;
        private bool isInVisualTree = false;

        public StatusBarIconWrapper()
        {
            notificationIndicator = new NotificationIndicator();
            notificationIndicator.DataContext = this;
        }

        public string BalloonTipText
        {
            get
            {
                return balloonTipText;
            }
            set
            {
                ThreadHelper.Generic.Invoke(() =>
                    SetAndRaisePropertyChanged(ref balloonTipText, value));
            }
        }

        public string Text
        {
            get
            {
                return text;
            }
            set
            {
                ThreadHelper.Generic.Invoke(() =>
                    SetAndRaisePropertyChanged(ref text, value));
            }
        }

        public bool HasEvents
        {
            get
            {
                return hasEvents;
            }

            set
            {
                ThreadHelper.Generic.Invoke(() =>
                    SetAndRaisePropertyChanged(ref hasEvents, value));
            }
        }

        public bool IsVisible
        {
            get
            {
                return notificationIndicator.Visibility == Visibility.Visible;
            }
            set
            {
                ThreadHelper.Generic.Invoke(() =>
                {
                    if (!isInVisualTree)
                    {
                        isInVisualTree = true;
                        VisualStudioStatusBarHelper.AddStatusBarIcon(notificationIndicator);
                    }

                    notificationIndicator.Visibility = value
                            ? Visibility.Visible
                            : Visibility.Collapsed;
                });
            }
        }

        public void OnBalloonTipClicked()
        {
            BalloonTipClick?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler BalloonTipClick;

        public void ShowBalloonTip()
        {
            ThreadHelper.Generic.Invoke(new Action(() =>
                notificationIndicator.ShowBalloonTip()));
        }
    }
}
