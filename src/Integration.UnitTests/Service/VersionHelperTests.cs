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
using SonarLint.VisualStudio.Integration.Service;

namespace SonarLint.VisualStudio.Integration.UnitTests.Service
{
    [TestClass]
    public class VersionHelperTests
    {
        [TestMethod]
        public void VersionHelper_Compare_NullVersionStrings_ThrowsException()
        {
            Exceptions.Expect<ArgumentNullException>(() => VersionHelper.Compare(null, "1.2.3"));
            Exceptions.Expect<ArgumentNullException>(() => VersionHelper.Compare("1.2.3", null));
        }

        [TestMethod]
        public void VersionHelper_Compare_InvalidVersionStrings_ThrowsException()
        {
            Exceptions.Expect<ArgumentException>(() => VersionHelper.Compare("notaversion", "1.2.3"));
            Exceptions.Expect<ArgumentException>(() => VersionHelper.Compare("1.2.3", "notaversion"));
        }

        [TestMethod]
        public void VersionHelper_Compare_SameVersionString_Release_AreSame()
        {
            // Act
            int result = VersionHelper.Compare("1.2.3", "1.2.3");

            // Assert
            result.Should().Be(0);
        }

        [TestMethod]
        public void VersionHelper_Compare_SameVersionString_Prerelease_AreSame()
        {
            // Test case 1: same 'dev string'
            // Act
            int result1 = VersionHelper.Compare("1.0-rc1", "1.0-rc2");

            // Assert
            result1.Should().Be(0);
        }

        [TestMethod]
        public void VersionHelper_Compare_ReleaseAndPrerelease_ComparesOnlyNumericParts()
        {
            // Act + Assert
            (VersionHelper.Compare("1.1", "1.2-beta") < 0).Should().BeTrue();
            (VersionHelper.Compare("1.1-beta", "1.2") < 0).Should().BeTrue();
        }

        [TestMethod]
        public void VersionHelper_Compare_NextMinorVersion()
        {
            // Act + Assert
            (VersionHelper.Compare("1.2", "1.3") < 0).Should().BeTrue();
            (VersionHelper.Compare("1.3", "1.2") > 0).Should().BeTrue();
        }
    }
}