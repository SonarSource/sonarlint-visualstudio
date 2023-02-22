/*
 * SonarQube Client
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

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarQube.Client.Models;

namespace SonarQube.Client.Tests.Models
{
    [TestClass]
    public class ServerExclusionsTests
    {
        [TestMethod]
        public void Equals_OtherObjIsNull_False()
        {
            var testSubject = new ServerExclusions(
                exclusions: new[] { "exclusion1" },
                globalExclusions: new[] { "global" },
                inclusions: new[] { "inclusion1" });

            testSubject.Equals(null).Should().BeFalse();
        }

        [TestMethod]
        public void Equals_ExclusionsAreDifferent_False()
        {
            var testSubject = new ServerExclusions(
                exclusions: new[] { "exclusion1" },
                globalExclusions: new[] { "global1" },
                inclusions: new[] { "inclusion1" });

            var other = new ServerExclusions(
                exclusions: new[] { "exclusion2" },
                globalExclusions: new[] { "global1" },
                inclusions: new[] { "inclusion1" });

            testSubject.Equals(other).Should().BeFalse();
        }

        [TestMethod]
        public void Equals_GlobalExclusionsAreDifferent_False()
        {
            var testSubject = new ServerExclusions(
                exclusions: new[] { "exclusion1" },
                globalExclusions: new[] { "global1" },
                inclusions: new[] { "inclusion1" });

            var other = new ServerExclusions(
                exclusions: new[] { "exclusion1" },
                globalExclusions: new[] { "global2" },
                inclusions: new[] { "inclusion1" });

            testSubject.Equals(other).Should().BeFalse();
        }

        [TestMethod]
        public void Equals_InclusionsAreDifferent_False()
        {
            var testSubject = new ServerExclusions(
                exclusions: new[] { "exclusion1" },
                globalExclusions: new[] { "global1" },
                inclusions: new[] { "inclusion1" });

            var other = new ServerExclusions(
                exclusions: new[] { "exclusion1" },
                globalExclusions: new[] { "global1" },
                inclusions: new[] { "inclusion2" });

            testSubject.Equals(other).Should().BeFalse();
        }

        [TestMethod]
        public void Equals_EverythingIsTheSame_True()
        {
            var testSubject = new ServerExclusions(
                exclusions: new[] { "exclusion1" },
                globalExclusions: new[] { "global1" },
                inclusions: new[] { "inclusion1" });

            var other = new ServerExclusions(
                exclusions: new[] { "exclusion1" },
                globalExclusions: new[] { "global1" },
                inclusions: new[] { "inclusion1" });

            testSubject.Equals(other).Should().BeTrue();
        }

        [TestMethod]
        public void Equals_SameReference_True()
        {
            var testSubject = new ServerExclusions(
                exclusions: new[] { "exclusion1" },
                globalExclusions: new[] { "global" },
                inclusions: new[] { "inclusion1" });

            testSubject.Equals(testSubject).Should().BeTrue();
        }
    }
}
