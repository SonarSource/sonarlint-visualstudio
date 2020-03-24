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

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core.CFamily;

namespace SonarLint.VisualStudio.Core.UnitTests.CFamily
{
    [TestClass]
    public class EffectiveRulesConfigCalculatorCacheTests
    {
        private EffectiveRulesConfigCalculator.RulesConfigCache testSubject;

        [TestInitialize]
        public void TestInitialize()
        {
            testSubject = new EffectiveRulesConfigCalculator.RulesConfigCache();
        }

        [TestMethod]
        public void Cache_DifferentSourceConfig_NotFound_AndEntryCleared()
        {
            var sourceConfig1 = new Mock<ICFamilyRulesConfig>().Object;
            var sourceSettings1 = new RulesSettings();
            var effectiveConfig1 = new Mock<ICFamilyRulesConfig>().Object;

            testSubject.Add("key1", sourceConfig1, sourceSettings1, effectiveConfig1);
            testSubject.CacheCount.Should().Be(1);

            // 1. Search for added item -> found
            testSubject.FindConfig("key1", sourceConfig1, sourceSettings1).Should().BeSameAs(effectiveConfig1);
            testSubject.CacheCount.Should().Be(1);

            // 2. Different source config -> not found
            testSubject.FindConfig("key1", new Mock<ICFamilyRulesConfig>().Object, sourceSettings1).Should().BeNull();
            testSubject.CacheCount.Should().Be(0);
        }

        [TestMethod]
        public void Cache_DifferentSourceSettings_NotFound_AndEntryCleared()
        {
            var sourceConfig1 = new Mock<ICFamilyRulesConfig>().Object;
            var sourceSettings1 = new RulesSettings();
            var effectiveConfig1 = new Mock<ICFamilyRulesConfig>().Object;

            testSubject.Add("key1", sourceConfig1, sourceSettings1, effectiveConfig1);
            testSubject.CacheCount.Should().Be(1);

            // 1. Search for added item -> found
            testSubject.FindConfig("key1", sourceConfig1, sourceSettings1).Should().BeSameAs(effectiveConfig1);
            testSubject.CacheCount.Should().Be(1);

            // 2. Different source settings -> not found
            testSubject.FindConfig("key1", sourceConfig1, new RulesSettings()).Should().BeNull();
            testSubject.CacheCount.Should().Be(0);
        }

        [TestMethod]
        public void Cache_MultipleEntries()
        {
            var sourceConfig1 = new Mock<ICFamilyRulesConfig>().Object;
            var sourceConfig2 = new Mock<ICFamilyRulesConfig>().Object;

            var sourceSettings1 = new RulesSettings();
            var sourceSettings2 = new RulesSettings();

            var effectiveConfig1 = new Mock<ICFamilyRulesConfig>().Object;
            var effectiveConfig2 = new Mock<ICFamilyRulesConfig>().Object;

            var testSubject = new EffectiveRulesConfigCalculator.RulesConfigCache();

            // 1. Empty cache -> cache miss
            testSubject.FindConfig("key1", sourceConfig1, sourceSettings1).Should().BeNull();

            // 2. Add first entry to cache
            testSubject.Add("key1", sourceConfig1, sourceSettings1, effectiveConfig1);
            testSubject.CacheCount.Should().Be(1);

            // 3. Find second language - not found
            testSubject.FindConfig("key2", sourceConfig2, sourceSettings2).Should().BeNull();

            // 4. Add second entry to cache
            testSubject.Add("key2", sourceConfig2, sourceSettings2, effectiveConfig2);
            testSubject.CacheCount.Should().Be(2);

            // 5. Check can find both entries
            testSubject.FindConfig("key1", sourceConfig1, sourceSettings1).Should().BeSameAs(effectiveConfig1);
            testSubject.FindConfig("key2", sourceConfig2, sourceSettings2).Should().BeSameAs(effectiveConfig2);
        }
    }
}
