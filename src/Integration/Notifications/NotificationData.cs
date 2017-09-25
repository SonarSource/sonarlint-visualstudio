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
using System.Runtime.Serialization;

namespace SonarLint.VisualStudio.Integration.Notifications
{
    [Serializable]
    public sealed class NotificationData : ISerializable
    {
        private const string Enabled_Key = "Enabled";
        private const string LastNotificationDate_Key = "LastNotificationDate";

        public bool IsEnabled { get; set; }

        public DateTimeOffset LastNotificationDate { get; set; }

        public NotificationData()
        {
            // Required for serialization.
        }

        private NotificationData(SerializationInfo info, StreamingContext context)
        {
            IsEnabled = (bool)info.GetValue(Enabled_Key, typeof(bool));
            LastNotificationDate = (DateTimeOffset)info.GetValue(LastNotificationDate_Key,
                typeof(DateTimeOffset));
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(Enabled_Key, IsEnabled, typeof(bool));
            info.AddValue(LastNotificationDate_Key, LastNotificationDate, typeof(DateTimeOffset));
        }
    }
}
