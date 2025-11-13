/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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

using System.Globalization;
using System.IO;
using System.Windows.Data;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace SonarLint.VisualStudio.IssueVisualization.Security.ReportView;

[ValueConversion(typeof(string), typeof(ImageMoniker))]
internal class FileNameToMonikerConverter : IValueConverter
{
    public object Convert(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {
        if (value is not string fileName)
        {
            return null;
        }

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".cs" => KnownMonikers.CSFileNode,
            ".vb" => KnownMonikers.VBFileNode,
            ".cpp" => KnownMonikers.CPPFileNode,
            ".c" or ".cc" or ".ccx" => KnownMonikers.CFile,
            ".h" or ".hpp" or ".hxx" => KnownMonikers.CPPHeaderFile,
            ".js" => KnownMonikers.JSScript,
            ".ts" => KnownMonikers.TSFileNode,
            ".json" => KnownMonikers.JSONScript,
            ".xml" => KnownMonikers.XMLFile,
            ".html" or ".htm" => KnownMonikers.HTMLFile,
            ".css" or ".scss" or ".less" => KnownMonikers.StyleSheet,
            ".png" or ".jpg" or ".jpeg" or ".gif" or ".bmp" or ".ico" or ".svg" => KnownMonikers.Image,
            ".txt" => KnownMonikers.TextFile,
            ".md" => KnownMonikers.MarkdownFile,
            ".yml" or ".yaml" => KnownMonikers.ConfigurationFile,
            ".config" => KnownMonikers.ConfigurationFile,
            ".dll" => KnownMonikers.Library,
            ".exe" => KnownMonikers.Application,
            _ => KnownMonikers.Document
        };
    }

    public object ConvertBack(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture) =>
        throw new NotImplementedException();
}
