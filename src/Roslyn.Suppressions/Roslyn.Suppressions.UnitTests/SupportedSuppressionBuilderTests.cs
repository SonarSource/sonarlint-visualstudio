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

using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SonarLint.VisualStudio.Roslyn.Suppressions.UnitTests
{
    [TestClass]
    public class SupportedSuppressionBuilderTests
    {
        [TestMethod]
        public void SupportedSuppressors_Instance_IsSingleton()
        {
            SupportedSuppressionsBuilder.Instance.Should().BeSameAs(SupportedSuppressionsBuilder.Instance);
        }

        [TestMethod]
        public void SupportedSuppressors_SuppressorsCreated()
        {
            var result = SupportedSuppressionsBuilder.Instance.Descriptors;

            // The exact number will vary every time a new version of either C# or VB analyzer is released
            // so we'll check a ball-park figure
            result.Count().Should().BeGreaterThan(400);
        }

        [TestMethod]
        public void SupportedSuppressors_ItemsAreNotNull()
        {
            var result = SupportedSuppressionsBuilder.Instance.Descriptors;

            result.Any(x => x == null).Should().BeFalse();
        }

        [TestMethod]
        public void SupportedSuppressions_NoDuplicates()
        {
            var result = SupportedSuppressionsBuilder.Instance.Descriptors;

            var distinctIdCount = result.Select(x => x.Id).Distinct().Count();
            result.Length.Should().Be(distinctIdCount);
        }

        [TestMethod]
        public void SupportedSuppressors_NoUtilityAnalyzers()
        {
            var result = SupportedSuppressionsBuilder.Instance.Descriptors;

            result.Any(x => x.Id.StartsWith("S9999")).Should().BeFalse();
        }
    }
}
