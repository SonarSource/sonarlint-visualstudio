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
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Notifications;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class NotificationDataTests
    {
        [TestMethod]
        public void TestSerializeDeserialize()
        {
            var data = new NotificationData
            {
                IsEnabled = false,
                LastNotificationDate = DateTimeOffset.FromUnixTimeSeconds(1410)
            };

            using (var stream = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(stream, data);

                stream.Position = 0;
                var deserialized = (NotificationData)formatter.Deserialize(stream);

                deserialized.IsEnabled.Should().Be(data.IsEnabled);
                deserialized.LastNotificationDate.Should().Be(data.LastNotificationDate);
            }
        }
    }
}