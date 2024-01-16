﻿/*
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

using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.CFamily.CMake;

namespace SonarLint.VisualStudio.CFamily.UnitTests.CMake
{
    [TestClass]
    public class CMakeHashCalculatorTests
    {
        [TestMethod]
        // Note: the test values were generated by create CMake projects in VS and changing the build root
        // property to include the ${(workspaceHash} and ${projectHash} macros.
        [DataRow(@"D:\features\CMake\VS2019\VS2019_CMakeProject1\CMakeLists.txt", "a22b1afb-db8a-4284-b249-ae89a35b8faa")]
        [DataRow(@"D:\features\CMake\Common\Common_CMakeProject1\CMakeLists.txt", "15a23e09-c50b-43ae-b0e1-a380a8ccd2b1")]
        [DataRow(@"D:\features\CMake\Common\XXXYYY\CMakeLists.txt", "a9840b6b-dbe5-4731-a2f4-2aa20fb8b352")]
        [DataRow(@"C:\Data\SunfireCatfish\CMakeLists.txt", "ec246df2-4701-4999-aae9-587cdbcd1a72")]
        [DataRow(@"C:\Data\Avalon\CoreEngine\DifferenceCalculator\Calculator\CMakeLists.txt", "7d6c0437-9f9a-4519-aa20-ed38ea36f35a")]
        public void CalculateVS2019_ReturnsExpectedValue(string input, string expected)
        {
            var expectedGuid = Guid.Parse(expected);

            var actual = CMakeHashCalculator.CalculateVS2019Guid(input);

            actual.Should().Be(expectedGuid);
        }
    }
}
