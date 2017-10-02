/*
 * SonarQube Client
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
using SonarQube.Client.Messages;

namespace SonarQube.Client.Models
{
    public class SonarQubeNotification
    {
        public string Category { get; set; }

        public string Message { get; set; }

        public Uri Link { get; set; }

        public DateTimeOffset Date { get; set; }

        public static SonarQubeNotification FromResponse(NotificationsResponse response)
        {
            return new SonarQubeNotification
            {
                Category = response.Category,
                Message = response.Message,
                Link = response.Link,
                Date = response.Date
            };
        }
    }
}
