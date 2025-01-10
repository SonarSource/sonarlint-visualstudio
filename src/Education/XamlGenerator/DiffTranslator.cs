/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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

using System.ComponentModel.Composition;
using System.Text;
using System.Xml;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;

namespace SonarLint.VisualStudio.Education.XamlGenerator
{
    public interface IDiffTranslator
    {
        /// <summary>
        /// Gets the difference between to strings in the format:
        ///     Regular text one
        ///     <Span Style="style">Text that <Span Style="SubStyle">is</Span> different</Span>
        ///     Regular Text two
        /// </summary>
        (string noncompliantXaml, string compliantXaml) GetDiffXaml(string noncompliantHtml, string compliantHtml);
    }

    [Export(typeof(IDiffTranslator))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class DiffTranslator : IDiffTranslator
    {
        private readonly IXamlWriterFactory xamlWriterFactory;

        [ImportingConstructor]
        public DiffTranslator(IXamlWriterFactory xamlWriterFactory)
        {
            this.xamlWriterFactory = xamlWriterFactory;
        }

        public (string noncompliantXaml, string compliantXaml) GetDiffXaml(string noncompliantHtml, string compliantHtml)
        {
            var resultDiff = SideBySideDiffBuilder.Diff(oldText: noncompliantHtml, newText: compliantHtml, ignoreWhiteSpace: false);

            var highlightedNonCompliant = HighlightLines(resultDiff.OldText.Lines, StyleResourceNames.NonCompliant_Diff, StyleResourceNames.Sub_NonCompliant_Diff);
            var highlightedCompliant = HighlightLines(resultDiff.NewText.Lines, StyleResourceNames.Compliant_Diff, StyleResourceNames.Sub_Compliant_Diff);

            return (highlightedNonCompliant, highlightedCompliant);
        }

        private string HighlightLines(List<DiffPiece> lines, StyleResourceNames style, StyleResourceNames subStyle)
        {
            var sb = new StringBuilder();
            var writer = xamlWriterFactory.Create(sb);

            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];

                if (string.IsNullOrEmpty(line.Text))
                {
                    continue;
                }

                if (line.Type != ChangeType.Unchanged)
                {
                    writer.WriteStartElement("Span");
                    writer.ApplyStyleToElement(style);

                    WriteSubPieces(writer, subStyle, line);

                    writer.WriteEndElement();
                }
                else
                {
                    writer.WriteRaw(line.Text);
                }

                // The differ removes the next line element as it returns a list of lines.
                // It needs to be added back for the xaml to render as intended.
                if (i < lines.Count - 1)
                {
                    writer.WriteRaw("\n");
                }
            }

            writer.Close();

            // The xml writer converts \n to \r\n which is not needed here.
            sb.Replace("\r\n", "\n");
            return sb.ToString();
        }

        private static void WriteSubPieces(XmlWriter writer, StyleResourceNames style, DiffPiece line)
        {
            if (line.SubPieces.Count == 0)
            {
                writer.WriteRaw(line.Text);
                return;
            }

            for (var i = 0; i < line.SubPieces.Count; i++)
            {
                var subPiece = line.SubPieces[i];
                if (IsSubPieceEmpty(subPiece))
                {
                    continue;
                }

                if (subPiece.Type != ChangeType.Unchanged)
                {
                    writer.WriteStartElement("Span");
                    writer.ApplyStyleToElement(style);
                    WriteSubPiece(writer, subPiece);
                    // group all consecutive changed sub-pieces in the same span
                    // this is needed to prevent html escape characters (e.g. &#39;) which end up in different sup-pieces to be split into different spans,
                    // which then won't be rendered correctly in the UI 
                    while (IsNextSubPieceChanged(line, i))
                    {
                        i++;
                        var nextSupPiece = line.SubPieces[i];
                        WriteSubPiece(writer, nextSupPiece);
                    }

                    writer.WriteEndElement();
                }
                else
                {
                    WriteSubPiece(writer, subPiece);
                }
            }
        }

        /// <summary>
        /// When writing a sub-piece, it's important to use the WriteRaw method to avoid escaping the special characters.
        /// The string is expected to already be html encoded, which means that when special characters (e.g. &) are escaped in html escape characters (e.g. &#39;),
        /// we end up with invalid characters that can't be interpreted (e.g. amp;#39;).
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="subPiece"></param>
        private static void WriteSubPiece(XmlWriter writer, DiffPiece subPiece) => writer.WriteRaw(subPiece.Text);

        private static bool IsSubPieceEmpty(DiffPiece subPiece) => string.IsNullOrEmpty(subPiece.Text);

        private static bool IsNextSubPieceChanged(DiffPiece line, int currentPosition)
        {
            if (currentPosition >= line.SubPieces.Count - 1)
            {
                return false;
            }
            var nextSupPiece = line.SubPieces[currentPosition + 1];
            return !IsSubPieceEmpty(nextSupPiece) && nextSupPiece.Type != ChangeType.Unchanged;
        }
    }
}
