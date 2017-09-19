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

namespace SonarLint.VisualStudio.Integration.Notifications
{
    public interface INotifyIcon : IDisposable
    {
        Icon Icon { get; set; }
        string Text { get; set; }
        bool Visible { get; set; }

        event EventHandler BalloonTipClicked;
        event EventHandler Click;
        event EventHandler DoubleClick;

        void ShowBalloonTip(int timeout, string tipTitle, string tipText);
    }
}
