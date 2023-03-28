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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml;

namespace SonarLint.VisualStudio.Education.XamlGenerator
{
    /// <summary>
    /// Translator for html content in rule definition to xaml
    /// </summary>
    /// <remarks>
    /// Our assumption is generated xaml will be partial and will be under a xaml root by caller
    /// This root will either be <FlowDocument> or <Section>
    /// </remarks>
    public interface IRuleHelpXamlTranslator
    {
        /// <summary>
        /// Translates the given partial html to Xaml
        /// </summary>
        /// <returns>Returns a non-inline xaml.</returns>
        /// <param name="htmlContent">Partial HTML which can be parsed as an XML</param>
        /// <remarks>
        /// Depending on the input return value can be a single block or multiple blocks
        /// But there can not be an inline item on the top level.
        /// </remarks>
        string TranslateHtmlToXaml(string htmlContent);
    }

    internal interface IRuleHelpXamlTranslatorFactory
    {
        IRuleHelpXamlTranslator Create();
    }

    [Export(typeof(IRuleHelpXamlTranslatorFactory))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class RuleHelpXamlTranslatorFactory : IRuleHelpXamlTranslatorFactory
    {
        private readonly IXamlWriterFactory xamlWriterFactory;

        [ImportingConstructor]
        public RuleHelpXamlTranslatorFactory(IXamlWriterFactory xamlWriterFactory)
        {
            this.xamlWriterFactory = xamlWriterFactory;
        }

        public IRuleHelpXamlTranslator Create()
        {
            return new RuleHelpXamlTranslator(xamlWriterFactory);
        }

        private sealed class RuleHelpXamlTranslator : IRuleHelpXamlTranslator
        {
            private readonly IXamlWriterFactory xamlWriterFactory;
            private XmlWriter writer;
            private XmlReader reader;

            /// <summary>
            /// Stack of currently open XAML elements
            /// </summary>
            /// <remarks>We need some information about the current structure so we can check whether some
            /// operations are valid e.g. can be we add text to the current element?</remarks>
            private Stack<XamlOutputElementInfo> outputXamlElementStack;

            /// <summary>
            /// Used to add background colour to alternate rows in a table
            /// </summary>
            private bool tableAlternateRow;

            public RuleHelpXamlTranslator(IXamlWriterFactory xamlWriterFactory)
            {
                this.xamlWriterFactory = xamlWriterFactory;
            }

            public string TranslateHtmlToXaml(string htmlContent)
            {
                var sb = new StringBuilder();
                writer = xamlWriterFactory.Create(sb);
                reader = CreateXmlReader(htmlContent);
                outputXamlElementStack = new Stack<XamlOutputElementInfo>();

                //We are putting this to simulate the root which will accept only Blocks. So we can add paragraph to inline elements to make them compatible.
                outputXamlElementStack.Push(new XamlOutputElementInfo("xaml root", false));

                try
                {
                    while (reader.Read())
                    {
                        switch (reader.NodeType)
                        {
                            case XmlNodeType.Element:
                                ProcessElement();
                                break;

                            case XmlNodeType.Text:
                                WriteText(reader.Value);
                                break;

                            case XmlNodeType.Whitespace:
                                writer.WriteString(reader.Value);
                                break;

                            case XmlNodeType.EndElement:
                                XamlOutputElementInfo xamlOutputElement;
                                do
                                {
                                    xamlOutputElement = outputXamlElementStack.Pop();
                                    writer.WriteEndElement();
                                } while (xamlOutputElement.HtmlElementName != reader.Name);

                                break;

                            default:
                                var message = string.Format(Resources.XamlBuilder_UnexpectedNodeError, reader.NodeType,
                                    reader.Name, reader.Value);
                                throw new InvalidDataException(message);
                        }
                    }

                    // We've processed all of the html elements.
                    // Now, the stack should only contain the root element, plus an extra
                    // block element if the first tag we processed was an Inline.
                    Debug.Assert(outputXamlElementStack.Count <= 2,
                        "Expecting at most 2 unclosed elements in the stack");
                    Debug.Assert((outputXamlElementStack.Count == 1 &&
                                  outputXamlElementStack.Peek().HtmlElementName == "xaml root")
                                 || (outputXamlElementStack.Count == 2 &&
                                     outputXamlElementStack.ToArray()[0].HtmlElementName == null &&
                                     outputXamlElementStack.ToArray()[1].HtmlElementName == "xaml root"),
                        "Unexpected items in final stack");

                    //root element will be on the calling class so opening and closing it should be handled there
                    while (outputXamlElementStack.Count > 1)
                    {
                        outputXamlElementStack.Pop();
                        writer.WriteEndElement();
                    }
                }
                finally
                {
                    reader.Close();
                    writer.Close();
                }

                return sb.ToString();
            }

            private void ProcessElement()
            {
                switch (reader.Name)
                {
                    case "a":
                        WriteInlineElementStart("Hyperlink");

                        var href = reader.GetAttribute("href");
                        writer.WriteAttributeString("NavigateUri", href);

                        break;

                    case "blockquote":
                        WriteBlockElementStart("Section");
                        writer.ApplyStyleToElement(StyleResourceNames.Blockquote_Section);

                        PushOutputElementInfo("blockquote", false);

                        break;

                    case "br":
                        // This is an empty element, so there is nothing to push onto the stack.
                        EnsureCurrentOutputSupportsInlines();
                        WriteEmptyElement("LineBreak");

                        break;

                    case "code":
                        WriteInlineElementStart("Span");
                        writer.ApplyStyleToElement(StyleResourceNames.Code_Span);

                        break;

                    case "em":
                        WriteInlineElementStart("Italic");
                        break;

                    case "h1":
                        WriteBlockElementStart("Paragraph");
                        writer.ApplyStyleToElement(StyleResourceNames.Heading1_Paragraph);

                        PushOutputElementInfo("h1", true);
                        break;

                    case "h2":
                        WriteBlockElementStart("Paragraph");
                        writer.ApplyStyleToElement(StyleResourceNames.Heading2_Paragraph);

                        PushOutputElementInfo("h2", true);
                        break;

                    case "h3":
                        WriteBlockElementStart("Paragraph");
                        writer.ApplyStyleToElement(StyleResourceNames.Heading3_Paragraph);

                        PushOutputElementInfo("h3", true);
                        break;

                    case "h4":
                        WriteBlockElementStart("Paragraph");
                        writer.ApplyStyleToElement(StyleResourceNames.Heading4_Paragraph);

                        PushOutputElementInfo("h4", true);
                        break;
                    case "h5":
                        WriteBlockElementStart("Paragraph");
                        writer.ApplyStyleToElement(StyleResourceNames.Heading5_Paragraph);

                        PushOutputElementInfo("h5", true);
                        break;
                    case "h6":
                        WriteBlockElementStart("Paragraph");
                        writer.ApplyStyleToElement(StyleResourceNames.Heading6_Paragraph);

                        PushOutputElementInfo("h6", true);
                        break;

                    case "li":
                        writer.WriteStartElement("ListItem");
                        PushOutputElementInfo("li", false);

                        break;

                    case "ol":
                        WriteBlockElementStart("List");
                        writer.ApplyStyleToElement(StyleResourceNames.OrderedList);

                        PushOutputElementInfo("ol", false);

                        break;

                    case "p":
                        WriteBlockElementStart("Paragraph");

                        PushOutputElementInfo("p", true);

                        break;

                    case "pre":
                        WriteBlockElementStart("Section");
                        writer.WriteAttributeString("xml", "space", null, "preserve");
                        writer.ApplyStyleToElement(StyleResourceNames.Pre_Section);

                        PushOutputElementInfo("pre", false);
                        break;

                    case "strong":
                        WriteInlineElementStart("Bold");

                        break;

                    case "ul":
                        WriteBlockElementStart("List");
                        writer.ApplyStyleToElement(StyleResourceNames.UnorderedList);

                        PushOutputElementInfo("ul", false);

                        break;

                    case "table":
                        WriteBlockElementStart("Table");
                        writer.ApplyStyleToElement(StyleResourceNames.Table);

                        PushOutputElementInfo("table", false);

                        break;

                    case "colgroup":
                        writer.WriteStartElement("Table.Columns");
                        PushOutputElementInfo("colgroup", false);

                        break;

                    case "col":
                        // This is an empty element, so there is nothing to push onto the stack.
                        WriteEmptyElement("TableColumn");

                        break;

                    case "thead":
                        writer.WriteStartElement("TableRowGroup");
                        writer.ApplyStyleToElement(StyleResourceNames.TableHeaderRowGroup);

                        PushOutputElementInfo("thead", false);

                        break;

                    case "tr":
                        writer.WriteStartElement("TableRow");
                        tableAlternateRow = !tableAlternateRow;

                        PushOutputElementInfo("tr", false);

                        break;

                    case "th":
                        writer.WriteStartElement("TableCell");
                        writer.ApplyStyleToElement(StyleResourceNames.TableHeaderCell);
                        PushOutputElementInfo("th", false);

                        break;

                    case "tbody":
                        tableAlternateRow = true;
                        writer.WriteStartElement("TableRowGroup");
                        PushOutputElementInfo("tbody", false);

                        break;

                    case "td":
                        writer.WriteStartElement("TableCell");
                        PushOutputElementInfo("td", false);

                        var cellStyle = tableAlternateRow
                            ? StyleResourceNames.TableBodyCellAlternateRow
                            : StyleResourceNames.TableBodyCell;
                        writer.ApplyStyleToElement(cellStyle);

                        break;

                    default:
                        Debug.Fail("Unexpected element type: " + reader.Name);
                        writer.WriteStartElement(reader.Name);
                        writer.WriteEndElement();
                        break;
                }
            }

            private static XmlReader CreateXmlReader(string data)
            {
                var settings = new XmlReaderSettings
                {
                    ConformanceLevel = ConformanceLevel.Fragment,
                    IgnoreWhitespace = false,
                };

                var stream = new StringReader(data);
                return XmlReader.Create(stream, settings);
            }

            

            private void WriteText(string text)
            {
                // If we are writing an inline element, we need a parent element that supports text directly.
                // e.g. <li> some text ... </li>            -> <li> does not support inlines directly.
                EnsureCurrentOutputSupportsInlines();

                // Note: we could explicitly wrap the text in a <Run>. However, that will happen implicitly
                // when the XAML is parsed, and it won't make any difference to the rendered output.
                writer.WriteString(text);
            }

            private void WriteInlineElementStart(string elementName)
            {
                // If we are writing an inline element, we need a parent element that supports inlines.

                // This might/might not be the case.
                // e.g. <li><p> some text ... </p><li>      -> <p> does support inlines, so we can just write the text
                // e.g. <li> <bold>some text ... </bold></li>            -> <li> does not support inlines directly.
                EnsureCurrentOutputSupportsInlines();

                writer.WriteStartElement(elementName);
                PushOutputElementInfo(reader.Name, true);
            }

            private void WriteBlockElementStart(string elementName)
            {
                EnsureCurrentOutputSupportsBlocks();
                writer.WriteStartElement(elementName);
            }

            /// <summary>
            /// Applies the specified style to the current element
            /// </summary>
            /// <remarks>
            /// Assumes the writer is in a state where we can write attributes i.e. just
            /// after writing the start element.
            /// <para>
            /// The resource is marked as a "DynamicResource".
            /// We can't mark it as a "StaticResource" unless we also define it in the XAML string
            /// (the XamlReader will complain it it can't find a referenced StaticResource when
            /// deserializing).
            /// Also, we want the resource references to be dynamic so they will automatically pick
            /// up resources defined in parent elements.
            /// </para>
            /// </remarks>
            private void PushOutputElementInfo(string htmlElementName, bool supportsInlines)
            {
                outputXamlElementStack.Push(new XamlOutputElementInfo(htmlElementName, supportsInlines));
            }

            private void WriteEmptyElement(string name, string value = null)
            {
                Debug.Assert(reader.IsEmptyElement);
                writer.WriteElementString(name, value);
            }

            private void EnsureCurrentOutputSupportsInlines()
            {
                var current = outputXamlElementStack.Peek();
                if (current.SupportsInlines)
                {
                    return;
                }

                // If the current XAML class doesn't support inlines then we assume that
                // it supports blocks, and add Paragraph.
                // Paragraph is a type of Block that supports Inlines.

                // Note that there are some XAML classes where this situation won't be valid
                // e.g. <Table>text
                // In this case, adding a Paragraph to a Table directly is not valid i.e. the
                // input HTML document is not valid. If the input document isn't valid then
                // we don't try to produce a valid XAML document from it.

                writer.WriteStartElement("Paragraph");
                PushOutputElementInfo(null, true);
            }

            private void EnsureCurrentOutputSupportsBlocks()
            {
                var current = outputXamlElementStack.Peek();
                if (current.SupportsBlocks)
                {
                    return;
                }

                // If we are in an element that supports inlines, we can't add another child element
                // that supports blocks because there are no WPF classes that do that.
                // Instead, all we can do is close the current element(s) recursively until we find
                // an existing output element that does support blocks.
                //
                // However, we can only walk back as far as the nearest parent output element that was
                // directly mapped to an html element (otherwise we'll fail later when we try to process
                // the matching closing html token).
                // In other words, we can only walk back up the stack closing "extra" elements we added
                // ourselves, that don't map to a specific html tag.

                // e.g. nested lists - c_s1749.desc
                // 1. <ol>
                // 2.     <li> type name, spelling of built-in types with more than one type-specifier:
                // 3.        <ol>
                // 4.             <li> signedness - <code>signed</code> or <code>unsigned</code> </li>

                // This produces the following XAML:
                // a.  <List>           <-- mapped to html <ol>
                // b.    <ListItem>     <-- mapped to html <li>
                //
                //                      // Next, we want to add the text from line 2. However, we
                //                      // can't add text to ListItem since it only accepts blocks.
                //                      // So, we add a Paragraph, which is a block that can contain
                //                      // Inlines e.g. text.
                //
                // c.      <Paragraph>  <-- extra element added by us, not mapped to an html element
                // d.        type name, spelling of..       <-- text from the html
                //
                //                      // Next, we want to handle the <ol> tag, which translates to
                //                      // a XAML "List". List is a block, which we can't add to a
                //                      // Paragraph. So, we need to walk back up the list of extra XAML
                //                      // elements we have opened and close them, until we reach an
                //                      // XAML class that supports Blocks.
                // e.      </Paragraph> <-- close the paragraph we opened to contain the text.
                //                      // The current XAML element is now the ListItem. This does
                //                      // accepts Blocks, so we can stop looking.
                // f.      <List>       <-- mapped to the nested html <li>

                // To summarise, in the example above, we add the extra <Paragraph> XAML opening tag,
                // since we need a paragraph to hold the text under the ListItem.
                // However, we then encounter the html <ol> element, which translates to another
                // XAML <List>. "List" is a block element, so we can't host it under <Paragraph>.
                // So, we close the <Paragraph> element and look at its parent, <ListItem>.
                // "ListItem" can contain blocks, so we can now add the new <List> opening tag.
                // Note: the <ListItem> in line 2 is as far back as we can check, since it is mapped
                // directly to an html element (<li>).

                while (current.HtmlElementName == null && current.SupportsInlines)
                {
                    writer.WriteEndElement();
                    outputXamlElementStack.Pop();

                    current = outputXamlElementStack.Peek();
                }

                if (current.SupportsInlines)
                {
                    throw new InvalidOperationException("Invalid state: can't find an element that supports blocks");
                }
            }
        }
    }
}
