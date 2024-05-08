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

using System.Text;
using SonarLint.VisualStudio.Education.XamlGenerator;

namespace SonarLint.VisualStudio.Education.UnitTests.XamlGenerator
{
    [TestClass]
    public class RuleHelpXamlTranslatorTests
    {
        [TestMethod]
        public void Factory_MefCtor_CheckExports()
        {
            MefTestHelpers.CheckTypeCanBeImported<RuleHelpXamlTranslatorFactory, IRuleHelpXamlTranslatorFactory>
                (MefTestHelpers.CreateExport<IXamlWriterFactory>(),
                MefTestHelpers.CreateExport<IDiffTranslator>());
        }

        [TestMethod]
        public void Factory_Create_NewInstanceEachTime()
        {
            var xamlWriterFactoryMock = new Mock<IXamlWriterFactory>();
            var testSubject = new RuleHelpXamlTranslatorFactory(xamlWriterFactoryMock.Object, Mock.Of<IDiffTranslator>());

            var o1 = testSubject.Create();
            var o2 = testSubject.Create();

            o1.Should().NotBeSameAs(o2);
            xamlWriterFactoryMock.Verify(x => x.Create(It.IsAny<StringBuilder>()), Times.Never);
        }

        [TestMethod]
        public void TranslateHtmlToXaml_CreatesNewWriter()
        {
            var xamlWriterFactoryActual = new XamlWriterFactory();
            var xamlWriterFactoryMock = new Mock<IXamlWriterFactory>();
            xamlWriterFactoryMock.Setup(x => x.Create(It.IsAny<StringBuilder>()))
                .Returns((StringBuilder sb) => xamlWriterFactoryActual.Create(sb));
            var testSubject = new RuleHelpXamlTranslatorFactory(xamlWriterFactoryMock.Object, Mock.Of<IDiffTranslator>()).Create();

            for (int i = 0; i < 5; i++)
            {
                testSubject.TranslateHtmlToXaml("inline Text");
                xamlWriterFactoryMock.Verify(x => x.Create(It.IsAny<StringBuilder>()), Times.Exactly(i + 1));
            }
        }

        [TestMethod]
        public void TranslateHtmlToXaml_InlineContent_AddsParagraph()
        {
            var testSubject = CreateTestSubject();

            var htmlText = "inline Text";

            var expectedText = "<Paragraph>inline Text</Paragraph>";

            var result = testSubject.TranslateHtmlToXaml(htmlText);

            result.Should().Be(expectedText);
        }

        [TestMethod]
        public void TranslateHtmlToXaml_BlockContent_Translates()
        {
            var testSubject = CreateTestSubject();

            var htmlText = @"<ul><li>some list item</li></ul>";

            var expectedText = @"<List Style=""{DynamicResource UnorderedList}"">
  <ListItem>
    <Paragraph>some list item</Paragraph>
  </ListItem>
</List>".Replace("\r\n", "\n").Replace("\n", "\r\n");

            var result = testSubject.TranslateHtmlToXaml(htmlText);

            result.Should().Be(expectedText);
        }

        [DataTestMethod]
        [DataRow(1)]
        [DataRow(2)]
        [DataRow(3)]
        [DataRow(4)]
        [DataRow(5)]
        [DataRow(6)]
        public void TranslateHtmlToXaml_HTagsAreHandled(int headerSize)
        {
            var testSubject = CreateTestSubject();

            var htmlText = $"<h{headerSize}>Text</h{headerSize}>";

            var expectedText = $"<Paragraph Style=\"{{DynamicResource Heading{headerSize}_Paragraph}}\">Text</Paragraph>";

            var result = testSubject.TranslateHtmlToXaml(htmlText);

            result.Should().Be(expectedText);
        }

        [TestMethod]
        public void TranslateHtmlToXaml_CrossReferenceRule_AddsHyperLink()
        {
            var testSubject = CreateTestSubject();

            var htmlText = "Texty text {rule:cpp:S1564} blabla";

            var expectedText = "<Paragraph>Texty text <Hyperlink NavigateUri=\"sonarlintrulecrossref://cpp/S1564\">cpp:S1564</Hyperlink> blabla</Paragraph>";

            var result = testSubject.TranslateHtmlToXaml(htmlText);

            result.Should().Be(expectedText);
        }

        [TestMethod]
        public void TranslateHtmlToXaml_Span_AddsSpan()
        {
            IRuleHelpXamlTranslator testSubject = CreateTestSubject();

            var htmlText = "<span>Some text</span>";

            var expectedText = @"<Paragraph>
  <Span>Some text</Span>
</Paragraph>";

            var result = testSubject.TranslateHtmlToXaml(htmlText);

            result.Should().Be(expectedText);
        }

        [TestMethod]
        public void TranslateHtmlToXaml_Sup_AddsSuperscriptBaselineAlignment()
        {
            IRuleHelpXamlTranslator testSubject = CreateTestSubject();

            var htmlText = "2<sup>53</sup>";

            var expectedText = @"<Paragraph>2<Span BaselineAlignment=""Superscript"">53</Span></Paragraph>";

            var result = testSubject.TranslateHtmlToXaml(htmlText);

            result.Should().Be(expectedText);
        }

        [TestMethod]
        public void TranslateHtmlToXaml_DataDiffExists_HighlightsCode()
        {
            var diffTranslator = new Mock<IDiffTranslator>();

            IRuleHelpXamlTranslator testSubject = CreateTestSubject(diffTranslator: diffTranslator.Object);

            var compliantText = "Same text 1\ndiff 1\nsame text 2";
            var nonCompliantText = "Same text 1\ndiff 2\nsame text 2";

            var compliantXaml = @"Same text 1
<Span Style=""{DynamicResource Compliant_Diff}"">diff 1</Span>
same text 2";

            var noncompliantXaml = @"Same text 1
<Span Style=""{DynamicResource NonCompliant_Diff}"">diff 2</Span>
same text 2";

            diffTranslator.Setup(d => d.GetDiffXaml(nonCompliantText, compliantText)).Returns((noncompliantXaml, compliantXaml));

            var htmlText = $"<pre data-diff-type=\"compliant\" data-diff-id=\"1\">{compliantText}</pre>\n<pre data-diff-type =\"noncompliant\" data-diff-id=\"1\">{nonCompliantText}</pre>";

            var expectedText = @"<Section xml:space=""preserve"" Style=""{DynamicResource Pre_Section}"">
  <Paragraph>Same text 1
<Span Style=""{DynamicResource Compliant_Diff}"">diff 1</Span>
same text 2</Paragraph>
</Section>
<Section xml:space=""preserve"" Style=""{DynamicResource Pre_Section}""><Paragraph>Same text 1
<Span Style=""{DynamicResource NonCompliant_Diff}"">diff 2</Span>
same text 2</Paragraph></Section>";

            var result = testSubject.TranslateHtmlToXaml(htmlText);

            result.Replace("\r\n", "\n").Should().Be(expectedText.Replace("\r\n", "\n"));
        }

        [TestMethod]
        public void TranslateHtmlToXaml_TwoCompliantCode_DoesNotHighlightCode()
        {
            var diffTranslator = new Mock<IDiffTranslator>();

            IRuleHelpXamlTranslator testSubject = CreateTestSubject(diffTranslator: diffTranslator.Object);

            var compliantText = "Same text 1\ndiff 1\nsame text 2";
            var nonCompliantText1 = "Same text 1\ndiff 2\nsame text 2";
            var nonCompliantText2 = "Same text 1\ndiff 3\nsame text 2";

            var compliantXaml = @"Same text 1
<Span Style=""{DynamicResource Compliant_Diff}"">diff 1</Span>
same text 2";

            var noncompliantXaml = @"Same text 1
<Span Style=""{DynamicResource NonCompliant_Diff}"">diff 2</Span>
same text 2";

            diffTranslator.Setup(d => d.GetDiffXaml(nonCompliantText1, compliantText)).Returns((noncompliantXaml, compliantXaml));

            var htmlText = $"<pre data-diff-type=\"compliant\" data-diff-id=\"1\">{compliantText}</pre>\n<pre data-diff-type =\"noncompliant\" data-diff-id=\"1\">{nonCompliantText1}</pre>\n<pre data-diff-type =\"noncompliant\" data-diff-id=\"1\">{nonCompliantText2}</pre>";

            var expectedText = @"<Section xml:space=""preserve"" Style=""{DynamicResource Pre_Section}"">
  <Paragraph>Same text 1
diff 1
same text 2</Paragraph>
</Section>
<Section xml:space=""preserve"" Style=""{DynamicResource Pre_Section}""><Paragraph>Same text 1
diff 2
same text 2</Paragraph></Section>
<Section xml:space=""preserve"" Style=""{DynamicResource Pre_Section}""><Paragraph>Same text 1
diff 3
same text 2</Paragraph></Section>";

            var result = testSubject.TranslateHtmlToXaml(htmlText);

            result.Replace("\r\n", "\n").Should().Be(expectedText.Replace("\r\n", "\n"));

            diffTranslator.Verify(d => d.GetDiffXaml(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void TranslateHtmlToXaml_TwoDiffs_HighlightsCodeCorrectly()
        {
            var diffTranslator = new Mock<IDiffTranslator>();
            IRuleHelpXamlTranslator testSubject = CreateTestSubject(diffTranslator: diffTranslator.Object);

            var compliantText1 = "Same text 1\ndiff 1";

            var noncompliantText1 = "Same text 1\ndiff 2";

            var compliantText2 = "diff 1\nsame 1";

            var noncompliantText2 = "diff 2\nsame 1";

            var compliantXaml1 = @"Same text 1
<Span Style=""{DynamicResource Compliant_Diff}"">diff 1</Span>";

            var noncompliantXaml1 = @"Same text 1
<Span Style=""{DynamicResource NonCompliant_Diff}"">diff 2</Span>";

            var compliantXaml2 = @"<Span Style=""{DynamicResource Compliant_Diff}"">diff 1</Span>
same 1";

            var noncompliantXaml2 = @"<Span Style=""{DynamicResource NonCompliant_Diff}"">diff 2</Span>
same 1";

            diffTranslator.Setup(d => d.GetDiffXaml(noncompliantText1, compliantText1)).Returns((noncompliantXaml1, compliantXaml1));
            diffTranslator.Setup(d => d.GetDiffXaml(noncompliantText2, compliantText2)).Returns((noncompliantXaml2, compliantXaml2));

            var htmlText = $"<pre data-diff-type=\"compliant\" data-diff-id=\"1\">{compliantText1}</pre><pre data-diff-type=\"compliant\" data-diff-id=\"2\">{compliantText2}</pre><pre data-diff-type=\"noncompliant\" data-diff-id=\"1\">{noncompliantText1}</pre><pre data-diff-type=\"noncompliant\" data-diff-id=\"2\">{noncompliantText2}</pre>";

            var expectedText = @"<Section xml:space=""preserve"" Style=""{DynamicResource Pre_Section}"">
  <Paragraph>Same text 1
<Span Style=""{DynamicResource Compliant_Diff}"">diff 1</Span></Paragraph>
</Section>
<Section xml:space=""preserve"" Style=""{DynamicResource Pre_Section}"">
  <Paragraph><Span Style=""{DynamicResource Compliant_Diff}"">diff 1</Span>
same 1</Paragraph>
</Section>
<Section xml:space=""preserve"" Style=""{DynamicResource Pre_Section}"">
  <Paragraph>Same text 1
<Span Style=""{DynamicResource NonCompliant_Diff}"">diff 2</Span></Paragraph>
</Section>
<Section xml:space=""preserve"" Style=""{DynamicResource Pre_Section}"">
  <Paragraph><Span Style=""{DynamicResource NonCompliant_Diff}"">diff 2</Span>
same 1</Paragraph>
</Section>";

            var result = testSubject.TranslateHtmlToXaml(htmlText);

            result.Replace("\r\n", "\n").Should().Be(expectedText.Replace("\r\n", "\n"));
        }

        [TestMethod]
        public void TranslateHtmlToXaml_SequentialCalls_HighlightsCorrectly()
        {
            var diffTranslator = new Mock<IDiffTranslator>();
            IRuleHelpXamlTranslator testSubject = CreateTestSubject(diffTranslator: diffTranslator.Object);

            var compliantText1 = "Same text 1\ndiff 1";

            var noncompliantText1 = "Same text 1\ndiff 2";

            var compliantText2 = "diff 1\nsame 1";

            var noncompliantText2 = "diff 2\nsame 1";

            var compliantXaml1 = @"Same text 1
<Span Style=""{DynamicResource Compliant_Diff}"">diff 1</Span>";

            var noncompliantXaml1 = @"Same text 1
<Span Style=""{DynamicResource NonCompliant_Diff}"">diff 2</Span>";

            var compliantXaml2 = @"<Span Style=""{DynamicResource Compliant_Diff}"">diff 1</Span>
same 1";

            var noncompliantXaml2 = @"<Span Style=""{DynamicResource NonCompliant_Diff}"">diff 2</Span>
same 1";

            diffTranslator.Setup(d => d.GetDiffXaml(noncompliantText1, compliantText1)).Returns((noncompliantXaml1, compliantXaml1));
            diffTranslator.Setup(d => d.GetDiffXaml(noncompliantText2, compliantText2)).Returns((noncompliantXaml2, compliantXaml2));

            var htmlText1 = $"<pre data-diff-type=\"compliant\" data-diff-id=\"1\">{compliantText1}</pre><pre data-diff-type=\"noncompliant\" data-diff-id=\"1\">{noncompliantText1}</pre>";
            var htmlText2 = $"<pre data-diff-type=\"compliant\" data-diff-id=\"1\">{compliantText2}</pre><pre data-diff-type=\"noncompliant\" data-diff-id=\"1\">{noncompliantText2}</pre>";

            var expectedText1 = @"<Section xml:space=""preserve"" Style=""{DynamicResource Pre_Section}"">
  <Paragraph>Same text 1
<Span Style=""{DynamicResource Compliant_Diff}"">diff 1</Span></Paragraph>
</Section>
<Section xml:space=""preserve"" Style=""{DynamicResource Pre_Section}"">
  <Paragraph>Same text 1
<Span Style=""{DynamicResource NonCompliant_Diff}"">diff 2</Span></Paragraph>
</Section>";

            var expectedText2 = @"<Section xml:space=""preserve"" Style=""{DynamicResource Pre_Section}"">
  <Paragraph><Span Style=""{DynamicResource Compliant_Diff}"">diff 1</Span>
same 1</Paragraph>
</Section>
<Section xml:space=""preserve"" Style=""{DynamicResource Pre_Section}"">
  <Paragraph><Span Style=""{DynamicResource NonCompliant_Diff}"">diff 2</Span>
same 1</Paragraph>
</Section>";

            var result1 = testSubject.TranslateHtmlToXaml(htmlText1);
            var result2 = testSubject.TranslateHtmlToXaml(htmlText2);

            result1.Replace("\r\n", "\n").Should().Be(expectedText1.Replace("\r\n", "\n"));
            result2.Replace("\r\n", "\n").Should().Be(expectedText2.Replace("\r\n", "\n"));
        }

        [TestMethod]
        public void TranslateHtmlToXaml_DataDiffWithAngleBracket_XMLParsable()
        {
            var diffTranslator = new Mock<IDiffTranslator>();

            IRuleHelpXamlTranslator testSubject = CreateTestSubject(diffTranslator: diffTranslator.Object);

            var compliantText = "#include &lt;vector&gt;";
            var nonCompliantText = "#include &lt;vector&gt;";

            var compliantXaml = @"#include &lt;vector&gt;";

            var noncompliantXaml = @"#include &lt;vector&gt;";

            diffTranslator.Setup(d => d.GetDiffXaml(compliantText, nonCompliantText)).Returns((noncompliantXaml, compliantXaml));

            var htmlText = $"<pre data-diff-type=\"compliant\" data-diff-id=\"1\">{compliantText}</pre>\n<pre data-diff-type =\"noncompliant\" data-diff-id=\"1\">{nonCompliantText}</pre>";

            var expectedText = @"<Section xml:space=""preserve"" Style=""{DynamicResource Pre_Section}"">
  <Paragraph>#include &lt;vector&gt;</Paragraph>
</Section>
<Section xml:space=""preserve"" Style=""{DynamicResource Pre_Section}""><Paragraph>#include &lt;vector&gt;</Paragraph></Section>";

            var result = testSubject.TranslateHtmlToXaml(htmlText);

            result.Replace("\r\n", "\n").Should().Be(expectedText.Replace("\r\n", "\n"));
        }

        private static IRuleHelpXamlTranslator CreateTestSubject(IXamlWriterFactory xamlWriterFactory = null, IDiffTranslator diffTranslator = null)
        {
            xamlWriterFactory ??= new XamlWriterFactory();
            diffTranslator ??= new DiffTranslator(new XamlWriterFactory());

            return new RuleHelpXamlTranslatorFactory(xamlWriterFactory, diffTranslator).Create();
        }
    }
}
