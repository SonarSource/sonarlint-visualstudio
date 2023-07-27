/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.Hotspots
{
    [TestClass]
    public class SonarQubeHotspotComparerTests
    {
        private const int SameNumber = 123;
        private const string SameKey = "key";

        [TestMethod]
        [DataRow(SameNumber, SameNumber, 0)]
        [DataRow(1, 2, -1)]
        [DataRow(2, 1, 1)]
        public void Compare_VaryStartLine(int startLine1, int startLine2, int expected)
        {
            var h1 = CreateServerHotspot(SameKey, startLine1, SameNumber);
            var h2 = CreateServerHotspot(SameKey, startLine2, SameNumber);

            CompareAndCheck(h1, h2, expected);
        }

        [TestMethod]
        [DataRow(SameNumber, SameNumber, 0)]
        [DataRow(1, 2, -1)]
        [DataRow(2, 1, 1)]
        public void Compare_VaryStartOffset(int startOffset1, int startOffset2, int expected)
        {
            var h1 = CreateServerHotspot(SameKey, SameNumber, startOffset1);
            var h2 = CreateServerHotspot(SameKey, SameNumber, startOffset2);

            CompareAndCheck(h1, h2, expected);
        }

        [TestMethod]
        [DataRow("aaa", "aaa", 0)]
        [DataRow("aaa", "zz", -1)]
        [DataRow("zzz", "aa", 1)]
        [DataRow("aaa", "AAA", -1)] // case-sensitive
        public void Compare_VaryHotspotKey(string key1, string key2, int expected)
        {
            var h1 = CreateServerHotspot(key1, SameNumber, SameNumber);
            var h2 = CreateServerHotspot(key2, SameNumber, SameNumber);

            CompareAndCheck(h1, h2, expected);
        }

        [TestMethod]
        [DataRow("aaa", "aaa", 0)]
        [DataRow("aaa", "zz", -1)]
        [DataRow("zzz", "aa", 1)]
        [DataRow("aaa", "AAA", -1)] // case-sensitive
        public void Compare_VaryHotspotKey_NullTextRanges(string key1, string key2, int expected)
        {
            var h1 = CreateServerHotspot(key1, null);
            var h2 = CreateServerHotspot(key2, null);

            CompareAndCheck(h1, h2, expected);
        }

        [TestMethod]
        public void CheckUsingInSortedSet()
        {
            // Sanity check that a sorted set behaves as expected
            var h1 = CreateServerHotspot("aaa", 1, 100);
            var h2 = CreateServerHotspot("aaa", 1, 200);

            var h3 = CreateServerHotspot("zzz", 100, 1);
            var h4 = CreateServerHotspot("aaa", 200, 1);

            var h5 = CreateServerHotspot("aaa", 200, 100);
            var h6 = CreateServerHotspot("zzz", 200, 100);

            Check(h1, h2, h3, h4, h5, h6);
            Check(h1, h2, h3, h4, h5, h6);
            Check(h2, h3, h6, h1, h5, h4);

            void Check(params SonarQubeHotspot[] allHotspots)
            {
                var set = new SortedSet<SonarQubeHotspot>(allHotspots, CreateTestSubject());
                set.ToList().Should().ContainInOrder(new[] { h1, h2, h3, h4, h5, h6 });
            }
        }

        private static void CompareAndCheck(SonarQubeHotspot h1,  SonarQubeHotspot h2, int expected)
        {
            var testSubject = CreateTestSubject();
            testSubject.Compare(h1, h2).Should().Be(expected);
        }

        private static LocalHotspotsStore.ServerHotspotComparer CreateTestSubject()
            => new LocalHotspotsStore.ServerHotspotComparer();

        private static SonarQubeHotspot CreateServerHotspot(
             string hotspotKey,
             int startLine,
             int startOffset)
        {
            var textRange = new IssueTextRange(startLine, startLine + 1, startOffset, startOffset + 1);
            return CreateServerHotspot(hotspotKey, textRange);
        }

        private static SonarQubeHotspot CreateServerHotspot(
             string hotspotKey,
             IssueTextRange textRange)
            => new SonarQubeHotspot(hotspotKey,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                DateTimeOffset.Now,
                DateTimeOffset.Now,
                null,
                textRange,
                null);
    }
}
