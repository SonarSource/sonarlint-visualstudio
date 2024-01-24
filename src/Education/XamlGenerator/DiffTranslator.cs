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

using System.Collections.Generic;
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

                if (string.IsNullOrEmpty(line.Text)) continue;

                if (line.Type != ChangeType.Unchanged)
                {
                    writer.WriteStartElement("Span");
                    writer.ApplyStyleToElement(style);

                    WriteSubPieces(writer, subStyle, line);

                    writer.WriteEndElement();
                }
                else
                {
                    writer.WriteString(line.Text);
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

        private void WriteSubPieces(XmlWriter writer, StyleResourceNames style, DiffPiece line)
        {
            if (line.SubPieces.Count == 0)
            {
                writer.WriteString(line.Text);
                return;
            }
           
            foreach (var subPiece in line.SubPieces)
            {
                if (string.IsNullOrEmpty(subPiece.Text)) continue;

                if (subPiece.Type != ChangeType.Unchanged)
                {
                    writer.WriteStartElement("Span");
                    writer.ApplyStyleToElement(style);
                    writer.WriteString(subPiece.Text);
                    writer.WriteEndElement();
                }
                else
                {
                    writer.WriteString(subPiece.Text);
                }
            }
        }
    }
}
