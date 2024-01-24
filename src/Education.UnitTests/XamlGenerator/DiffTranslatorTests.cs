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

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Education.XamlGenerator;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Education.UnitTests.XamlGenerator
{
    [TestClass]
    public class DiffTranslatorTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<DiffTranslator, IDiffTranslator>(
                MefTestHelpers.CreateExport<IXamlWriterFactory>());
        }

        [TestMethod]
        public void GetDiffXaml_AppliesCorrectStyle()
        {
            var testSubject = CreateTestSubject();

            (string result1, string result2) = testSubject.GetDiffXaml("input1", "input2");

            var expectedResult1 = @"<Span Style=""{DynamicResource NonCompliant_Diff}"">
  <Span Style=""{DynamicResource Sub_NonCompliant_Diff}"">input1</Span>
</Span>";
            var expectedResult2 = @"<Span Style=""{DynamicResource Compliant_Diff}"">
  <Span Style=""{DynamicResource Sub_Compliant_Diff}"">input2</Span>
</Span>";

            result1.Replace("\r\n", "\n").Should().Be(expectedResult1.Replace("\r\n", "\n"));
            result2.Replace("\r\n", "\n").Should().Be(expectedResult2.Replace("\r\n", "\n"));
        }

        [TestMethod]
        public void GetDiffXaml_DiffTextMultipleLines_KeepsFormat()
        {
            var input1 = "same\n" +
                         "same one\n" +
                         "same same \n" +
                         "one";
            var input2 = "same\n" +
                         "same two\n" +
                         "same same \n" +
                         "two";

            var testSubject = CreateTestSubject();

            (string result1, string result2) = testSubject.GetDiffXaml(input1, input2);

            result1.Should().BeEquivalentTo(
                "same\n" +
                "<Span Style=\"{DynamicResource NonCompliant_Diff}\">same <Span Style=\"{DynamicResource Sub_NonCompliant_Diff}\">one</Span></Span>\n" +
                "same same \n" +
                "<Span Style=\"{DynamicResource NonCompliant_Diff}\"><Span Style=\"{DynamicResource Sub_NonCompliant_Diff}\">one</Span></Span>");
            result2.Should().BeEquivalentTo(
                "same\n" +
                "<Span Style=\"{DynamicResource Compliant_Diff}\">same <Span Style=\"{DynamicResource Sub_Compliant_Diff}\">two</Span></Span>\n" +
                "same same \n" +
                "<Span Style=\"{DynamicResource Compliant_Diff}\"><Span Style=\"{DynamicResource Sub_Compliant_Diff}\">two</Span></Span>");
        }

        [TestMethod]
        public void GetDiffXaml_IgnoreNullOrEmptyStrings()
        {
            // This specific input can cause Diffplex to return DiffPieces that are null or empty strings.
            // Without correction in the DiffTranslator the result would add two extra spans after the first line break 
            // which would look like this:
            // "// ...\n<Span Style=\"{DynamicResource Compliant_Diff}\"><Span Style=\"{DynamicResource Sub_Compliant_Diff}\">}}</Span><Span Style=\"{DynamicResource Sub_Compliant_Diff}\" /></Span>"
            // This test is to ensure the DiffTranslator takes care of not adding these extra spans.
            var input1 = "// ...\r\n}";
            var input2 = "// ...\r\n}}";

            var testSubject = CreateTestSubject();

            (string result1, string result2) = testSubject.GetDiffXaml(input1, input2);

            result1.Should().BeEquivalentTo("// ...\n<Span Style=\"{DynamicResource NonCompliant_Diff}\"><Span Style=\"{DynamicResource Sub_NonCompliant_Diff}\">}</Span></Span>");
            result2.Should().BeEquivalentTo("// ...\n<Span Style=\"{DynamicResource Compliant_Diff}\"><Span Style=\"{DynamicResource Sub_Compliant_Diff}\">}}</Span></Span>");
        }

        [TestMethod]
        public void GetDiffXaml_TextIsSame_ResultIsSame()
        {
            var input = "same\nsame";

            var testSubject = CreateTestSubject();

            (string result1, string result2) = testSubject.GetDiffXaml(input, input);

            result1.Should().BeEquivalentTo(input);
            result2.Should().BeEquivalentTo(result1);
        }

        [TestMethod]
        public void GetDiffXaml_LineHasNoSubPieces_StillCorrectResult()
        {
            var input1 = "} if (condition2) {";
            var input2 = "}\r\n\r\nif (condition2) {";

            var testSubject = CreateTestSubject();

            (string result1, string result2) = testSubject.GetDiffXaml(input1, input2);

            result1.Should().BeEquivalentTo("<Span Style=\"{DynamicResource NonCompliant_Diff}\">\n  <Span Style=\"{DynamicResource Sub_NonCompliant_Diff}\">} </Span>\n  <Span Style=\"{DynamicResource Sub_NonCompliant_Diff}\">if</Span>\n  <Span Style=\"{DynamicResource Sub_NonCompliant_Diff}\"> (</Span>\n  <Span Style=\"{DynamicResource Sub_NonCompliant_Diff}\">condition2</Span>\n  <Span Style=\"{DynamicResource Sub_NonCompliant_Diff}\">) {</Span>\n</Span>\n");
            result2.Should().BeEquivalentTo("<Span Style=\"{DynamicResource Compliant_Diff}\">\n  <Span Style=\"{DynamicResource Sub_Compliant_Diff}\">}</Span>\n</Span>\n<Span Style=\"{DynamicResource Compliant_Diff}\">if (condition2) {</Span>");
        }

        private static DiffTranslator CreateTestSubject(IXamlWriterFactory xamlWriterFactory = null)
        {
            xamlWriterFactory ??= new XamlWriterFactory();
            var testSubject = new DiffTranslator(xamlWriterFactory);

            return testSubject;
        }
    }
}
