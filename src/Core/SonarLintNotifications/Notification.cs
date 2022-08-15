﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2022 SonarSource SA
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

namespace SonarLint.VisualStudio.Core.SonarLintNotifications
{
    public interface INotification
    {
        Guid NotificationId { get; }
        string Message { get; }
        IEnumerable<INotificationAction> Actions { get; }
    }

    public class Notification : INotification
    {
        public Notification(Guid notificationId, string message, IEnumerable<INotificationAction> actions)
        {
            NotificationId = notificationId;
            Message = message;
            Actions = actions;
        }

        public Guid NotificationId { get; }
        public string Message { get; }
        public IEnumerable<INotificationAction> Actions { get; }
    }
}
