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

namespace SonarLint.VisualStudio.Integration
{
    public static class DateTimeOffsetExtensions
    {
        public static bool IsSameDayOld(this DateTimeOffset date, DateTimeOffset other) =>
            Math.Abs(date.Date.Subtract(other.Date).TotalDays) < 1;

        /// <summary>
        /// Returns true if the two date-times fall in the same calendar day in
        /// the specified time-zone, otherwise false
        /// </summary>
        /// <param name="timeZoneInfo">(Optional) time-zone to used to determine the calendar day.
        /// Defaults to the local time-zone.</param>
        public static bool IsSameDay(this DateTimeOffset date, DateTimeOffset other, TimeZoneInfo timeZoneInfo = null)
        {
            if (timeZoneInfo == null)
            {
                timeZoneInfo = TimeZoneInfo.Local;
            }

            var localDate1 = TimeZoneInfo.ConvertTime(date, timeZoneInfo);
            var localDate2 = TimeZoneInfo.ConvertTime(other, timeZoneInfo);

            return Math.Abs(localDate1.Subtract(localDate2).TotalDays) < 1 && (localDate1.Day == localDate2.Day);
        }


        public static long HoursPassedSince(this DateTimeOffset date, DateTimeOffset other) =>
            (long)date.Subtract(other).TotalHours;

        public static long DaysPassedSince(this DateTimeOffset date, DateTimeOffset other) =>
            (long)date.Subtract(other).TotalDays;
    }
}
