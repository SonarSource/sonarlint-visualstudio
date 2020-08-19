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
using Moq;
using SonarLint.VisualStudio.Core.Helpers;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarLint.VisualStudio.IssueVisualization.Editor;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor
{
    [TestClass]
    public class LineHashCalculatorTests
    {
        private Mock<IChecksumCalculator> checksumCalculatorMock;

        private LineHashCalculator testSubject;

        [TestInitialize]
        public void TestInitialize()
        {
            checksumCalculatorMock = new Mock<IChecksumCalculator>();

            testSubject = new LineHashCalculator(checksumCalculatorMock.Object);
        }

        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            // Act & Assert
            MefTestHelpers.CheckTypeCanBeImported<LineHashCalculator, ILineHashCalculator>(null, null);
        }

        [TestMethod]
        [DataRow("")]
        [DataRow(null)]
        public void Calculate_NoText_Null(string text)
        {
            var result = testSubject.Calculate(text, 10);

            result.Should().BeNullOrEmpty();

            checksumCalculatorMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Calculate_TextHasOneLine_GivenLineIsOne_LineHashed()
        {
            const string text = "one line";
            const int line = 1;

            const string expectedHash = "hashed line";
            checksumCalculatorMock.Setup(x => x.Calculate(text)).Returns(expectedHash);

            var result = testSubject.Calculate(text, line);

            result.Should().Be(expectedHash);
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(2)]
        public void Calculate_GivenLineIsNotInTextRange_Null(int line)
        {
            const string text = "one line";

            var result = testSubject.Calculate(text, line);

            result.Should().BeNullOrEmpty();

            checksumCalculatorMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        [DataRow(1, "one line")]
        [DataRow(2, "second line")]
        [DataRow(3, "third line")]
        public void Calculate_GivenLineIsInTextRange_LineHashed(int line, string expectedLineToHash)
        {
            var text = string.Join(Environment.NewLine, "one line", "second line", "third line");

            const string expectedHash = "hashed line";
            checksumCalculatorMock.Setup(x => x.Calculate(expectedLineToHash)).Returns(expectedHash);

            var result = testSubject.Calculate(text, line);

            result.Should().Be(expectedHash);
        }
    }
}
