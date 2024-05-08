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

using Microsoft.VisualStudio.Text;
using SonarLint.VisualStudio.Infrastructure.VS.Editor;
using SonarQube.Client;

namespace SonarLint.VisualStudio.Infrastructure.VS.UnitTests.Editor
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
        public void Calculate_NullTextSnapshot_Null()
        {
            var result = testSubject.Calculate(null, 10);

            result.Should().BeNull();

            checksumCalculatorMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Calculate_NullTextLine_Null()
        {
            const int oneBasedNonExistingLineNumber = 100;

            var textSnapshot = CreateTextSnapshot(oneBasedNonExistingLineNumber, line: null);

            var result = testSubject.Calculate(textSnapshot, oneBasedNonExistingLineNumber);

            result.Should().BeNull();

            checksumCalculatorMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Calculate_NullTextLineContent_Null()
        {
            const int oneBasedLineNumber = 2;

            var textSnapshot = CreateTextSnapshot(oneBasedLineNumber, lineText:null);

            var result = testSubject.Calculate(textSnapshot, oneBasedLineNumber);

            result.Should().BeNull();

            checksumCalculatorMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(-1)]
        public void Calculate_InvalidLineNumber_Null(int oneBasedLineNumber)
        {
            var textSnapshot = CreateTextSnapshot(oneBasedLineNumber, "some text that shouldn't be checked");

            var result = testSubject.Calculate(textSnapshot, oneBasedLineNumber);

            result.Should().BeNull();

            checksumCalculatorMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        [DataRow(1)]
        [DataRow(10)]
        public void Calculate_ValidLine_LineHashed(int oneBasedLineNumber)
        {
            const string text = "some text";

            const string expectedHash = "hashed line";
            checksumCalculatorMock.Setup(x => x.Calculate(text)).Returns(expectedHash);

            var textSnapshot = CreateTextSnapshot(oneBasedLineNumber, text);

            var result = testSubject.Calculate(textSnapshot, oneBasedLineNumber);

            result.Should().Be(expectedHash);
        }

        private ITextSnapshot CreateTextSnapshot(int oneBasedLineNumber, ITextSnapshotLine line)
        {
            var textSnapshot = new Mock<ITextSnapshot>();
            textSnapshot.Setup(x => x.GetLineFromLineNumber(oneBasedLineNumber - 1)).Returns(line);

            return textSnapshot.Object;
        }

        private ITextSnapshot CreateTextSnapshot(int oneBasedLineNumber, string lineText)
        {
            var line = new Mock<ITextSnapshotLine>();
            line.Setup(x => x.GetText()).Returns(lineText);

            var textSnapshot = new Mock<ITextSnapshot>();
            textSnapshot.Setup(x => x.GetLineFromLineNumber(oneBasedLineNumber - 1)).Returns(line.Object);

            return textSnapshot.Object;
        }
    }
}
