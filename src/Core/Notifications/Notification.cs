/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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
using System.Collections.Generic;

namespace SonarLint.VisualStudio.Core.Notifications
{
    public interface INotification
    {
        string Id { get; }
        string Message { get; }
        IEnumerable<INotificationAction> Actions { get; }

        bool ShowOncePerSession { get; }
    }

    public class Notification : INotification
    {
        public Notification(string id, string message, IEnumerable<INotificationAction> actions, bool showOncePerSession = true)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Message = message ?? throw new ArgumentNullException(nameof(message));
            Actions = actions ?? throw new ArgumentNullException(nameof(actions));
            ShowOncePerSession = showOncePerSession;
        }

        public string Id { get; }
        public string Message { get; }
        public IEnumerable<INotificationAction> Actions { get; }
        public bool ShowOncePerSession { get; }
    }
}
