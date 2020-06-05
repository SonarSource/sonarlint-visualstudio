/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SonarLint.VisualStudio.Integration
{
    [TestClass]
    public class DateTimeOffsetExtensionsTest
    {
        private static readonly DateTimeOffset today_00h_00m = new DateTimeOffset(new DateTime(2017, 10, 20));
        private static readonly DateTimeOffset today_00h_01m = today_00h_00m.AddMinutes(1);
        private static readonly DateTimeOffset today_23h_59m = today_00h_00m.AddHours(23).AddMinutes(59);
        private static readonly DateTimeOffset yesterday_00h_00m = new DateTimeOffset(new DateTime(2017, 10, 19));
        private static readonly DateTimeOffset yesterday_23h_59m = yesterday_00h_00m.AddHours(23).AddMinutes(59);
        private static readonly DateTimeOffset tomorrow_00h_00m = new DateTimeOffset(new DateTime(2017, 10, 21));
        private static readonly DateTimeOffset tomorrow_23h_59m = tomorrow_00h_00m.AddHours(23).AddMinutes(59);


        private const string FixedDate = "2020-06-01T";
        private const string FixedTime0430 = "04:30:00.000";

        private const string UTC_0430 = FixedDate + FixedTime0430 + "+00:00";
        private const string Sydney_0430 = FixedDate + FixedTime0430 + "+08:00";
        private const string California_0430 = FixedDate + FixedTime0430 + "-8:00";

        [TestMethod]
        public void DaysPassedSince()
        {
            today_00h_00m.DaysPassedSince(yesterday_23h_59m).Should().Be(0);
            today_00h_00m.DaysPassedSince(yesterday_00h_00m).Should().Be(1);
            today_00h_00m.DaysPassedSince(today_00h_00m).Should().Be(0);
            today_00h_00m.DaysPassedSince(today_00h_01m).Should().Be(0);
            today_00h_00m.DaysPassedSince(today_23h_59m).Should().Be(0);
            today_00h_00m.DaysPassedSince(tomorrow_00h_00m).Should().Be(-1);
            today_00h_00m.DaysPassedSince(tomorrow_23h_59m).Should().Be(-1);
            tomorrow_23h_59m.DaysPassedSince(yesterday_00h_00m).Should().Be(2);
        }

        [TestMethod]
        public void HoursPassedSince()
        {
            today_00h_00m.HoursPassedSince(yesterday_23h_59m).Should().Be(0);
            today_00h_00m.HoursPassedSince(yesterday_00h_00m).Should().Be(24);
            today_00h_00m.HoursPassedSince(today_00h_00m).Should().Be(0);
            today_00h_00m.HoursPassedSince(today_00h_01m).Should().Be(0);
            today_00h_00m.HoursPassedSince(today_23h_59m).Should().Be(-23);
            today_00h_00m.HoursPassedSince(tomorrow_00h_00m).Should().Be(-24);
            today_00h_00m.HoursPassedSince(tomorrow_23h_59m).Should().Be(-47);
            tomorrow_23h_59m.HoursPassedSince(yesterday_00h_00m).Should().Be(71);
        }

        [TestMethod]
        public void IsSameDay_InputsInSameTimeZone_DefaultLocalTimeZone()
        {
            today_00h_00m.IsSameDay(yesterday_23h_59m).Should().BeFalse();
            today_00h_00m.IsSameDay(yesterday_00h_00m).Should().BeFalse();
            today_00h_00m.IsSameDay(today_00h_00m).Should().BeTrue();
            today_00h_00m.IsSameDay(today_00h_01m).Should().BeTrue();
            today_00h_00m.IsSameDay(today_23h_59m).Should().BeTrue();
            today_00h_00m.IsSameDay(tomorrow_00h_00m).Should().BeFalse();
            today_00h_00m.IsSameDay(tomorrow_23h_59m).Should().BeFalse();
        }

        [TestMethod]
        [DataRow(-10, false)]// Hawaii -> UTC_0430 is a different day
        [DataRow(-5, false)] // Cuba -> UTC_0430 is a different day
        [DataRow(-3, true)] // Brasilia -> same day
        [DataRow(0, true)] // UTC -> same day
        [DataRow(5, true)] // Karachi -> same day
        [DataRow(13, false)] // Samoa -> UTC_1030 is a different day
        public void IsSameDay_InputsInSameTimeZone_DifferentLocalTimeZone(int timeZoneOffset, bool expected)
        {
            const string UTC_1130 = FixedDate + "11:30:00.000+00:00";

            // Whether or not they fall in the same calendar day depends on the time-zone
            // we pick as the "local" time-zone
            CheckIsSameDay(UTC_0430, UTC_1130, timeZoneOffset, expected);
        }

        [TestMethod]
        // UTC and California are 8 hours apart
        [DataRow(UTC_0430, California_0430, -10, false)]
        [DataRow(UTC_0430, California_0430, 0, true)]
        [DataRow(UTC_0430, California_0430, 10, true)]

        // Also 8 hours apart, but in the other direction
        [DataRow(UTC_0430, Sydney_0430, -10, true)]
        [DataRow(UTC_0430, Sydney_0430, 0, false)]
        [DataRow(UTC_0430, Sydney_0430, 10, true)]

        // 16-hour difference
        [DataRow(Sydney_0430, California_0430, -10, false)]
        [DataRow(Sydney_0430, California_0430, 0, false)]
        [DataRow(Sydney_0430, California_0430, 8, true)]
        public void IsSameDay_InputsInSameTimeZone_DefaultLocalTimeZone(string dateTime1, string dateTime2, int timeZoneOffset, bool expected) =>
            CheckIsSameDay(dateTime1, dateTime2, timeZoneOffset, expected);

        private static void CheckIsSameDay(string dateTime1, string dateTime2, int timeZoneOffset, bool expected)
        {
            var timeZone = TimeZoneInfo.CreateCustomTimeZone("test time zone", TimeSpan.FromHours(timeZoneOffset), "test time zone", "test time zone");

            var dt1 = DateTimeOffset.Parse(dateTime1);
            var dt2 = DateTimeOffset.Parse(dateTime2);

            // Results should be symmetric
            dt1.IsSameDay(dt2, timeZone).Should().Be(expected);
            dt2.IsSameDay(dt1, timeZone).Should().Be(expected);
        }
    }
}
