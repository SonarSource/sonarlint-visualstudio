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
        private static readonly DateTimeOffset yesterday_00h_01m = yesterday_00h_00m.AddMinutes(1);
        private static readonly DateTimeOffset yesterday_23h_59m = yesterday_00h_00m.AddHours(23).AddMinutes(59);
        private static readonly DateTimeOffset tomorrow_00h_00m = new DateTimeOffset(new DateTime(2017, 10, 21));
        private static readonly DateTimeOffset tomorrow_00h_01m = tomorrow_00h_00m.AddMinutes(1);
        private static readonly DateTimeOffset tomorrow_23h_59m = tomorrow_00h_00m.AddHours(23).AddMinutes(59);

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
        public void IsSameDay()
        {
            today_00h_00m.IsSameDay(yesterday_23h_59m).Should().BeFalse();
            today_00h_00m.IsSameDay(yesterday_00h_00m).Should().BeFalse();
            today_00h_00m.IsSameDay(today_00h_00m).Should().BeTrue();
            today_00h_00m.IsSameDay(today_00h_01m).Should().BeTrue();
            today_00h_00m.IsSameDay(today_23h_59m).Should().BeTrue();
            today_00h_00m.IsSameDay(tomorrow_00h_00m).Should().BeFalse();
            today_00h_00m.IsSameDay(tomorrow_23h_59m).Should().BeFalse();
        }
    }
}
