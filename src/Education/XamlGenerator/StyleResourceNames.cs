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

namespace SonarLint.VisualStudio.Education.XamlGenerator
{
    /// <summary>
    /// Style names used by the XAML generator
    /// </summary>
    /// <remarks>The generator refers to resources with these names, but does not generate them itself
    /// i.e. it expects them to be defined in a resource dictionary in whatever hosts the generated XAML.
    /// The enum name includes the target type of the XAML element if it isn't obvious
    /// e.g. <see cref="StyleResourceNames.Code_Span"/> is used for code elements, rendered in a XAML Span.
    /// e.g. <see cref="StyleResourceNames.Heading2_Paragraph"/> is used to write a header in a XAML Paragraph.</remarks>
    public enum StyleResourceNames
    {
        Pre_Section,                // html <pre>
        Blockquote_Section,         // html <blockquote>, rendered in a XAML <Section>
        Heading1_Paragraph,         // html <h1>, rendered in a XAML <Paragraph>
        Heading2_Paragraph,         // html <h2>, rendered in a XAML <Paragraph>
        Heading3_Paragraph,         // html <h3>, rendered in a XAML <Paragraph>
        Heading4_Paragraph,         // html <h4>, rendered in a XAML <Paragraph>
        Heading5_Paragraph,         // html <h5>, rendered in a XAML <Paragraph>
        Heading6_Paragraph,         // html <h6>, rendered in a XAML <Paragraph>
        Table,                      // html <table>, rendered in a XAML <Table>
        TableHeaderRowGroup,        // html <thead>, rendered in a XAML <TableRowGroup>
        TableHeaderCell,            // html <th>, rendered in a XAML <TableCell>
        TableBodyCell,              // html <td>, rendered in a XAML <TableCell>
        TableBodyCellAlternateRow,  // html <td>, rendered in a XAML <TableCell>. Applied to alternate rows of the table body.
        OrderedList,                // html <ol>, rendered is a XAML <List>
        UnorderedList,              // html <ul>, rendered is a XAML <List>
        Code_Span,                  // html <code>, rendered is a XAML <Span>
        Title_Paragraph,            // rule title (name) - <paragraph>
        SubtitleElement_Span,       // element under the title e.g. issue type, severity, rule id
        SubtitleElement_Image       // image in a subtitle element
    }
}
