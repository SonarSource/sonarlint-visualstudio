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
using System.Windows.Media;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.IssueVisualization.Security.HotspotsList;
using SonarLint.VisualStudio.IssueVisualization.Security.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.HotspotsList
{
    [TestClass]
    public class PriorityToBackgroundConverterTests
    {
        [TestMethod]
        public void ConvertBack_NotImplementedException()
        {
            var testSubject = new PriorityToBackgroundConverter();

            Action act = () => testSubject.ConvertBack(null, null, null, null);

            act.Should().Throw<NotImplementedException>("ConvertBack is not required");
        }

        [TestMethod]
        public void Convert_HighPriority_Converted()
        {
            AssertConversion(HotspotPriority.High, PriorityToBackgroundConverter.HighPriorityBrush);
        }

        [TestMethod]
        public void Convert_MediumPriority_Converted()
        {
            AssertConversion(HotspotPriority.Medium, PriorityToBackgroundConverter.MediumPriorityBrush);
        }

        [TestMethod]
        public void Convert_LowPriority_Converted()
        {
            AssertConversion(HotspotPriority.Low, PriorityToBackgroundConverter.LowPriorityBrush);
        }

        [TestMethod]
        public void Convert_UnknownPriority_Converted()
        {
            AssertConversion((HotspotPriority)12345678, Brushes.Transparent);
        }

        private void AssertConversion(HotspotPriority priority, SolidColorBrush expectedColor)
        {
            var testSubject = new PriorityToBackgroundConverter();
            var result = testSubject.Convert(priority, null, null, null);

            result.Should().Be(expectedColor);
        }
    }
}
