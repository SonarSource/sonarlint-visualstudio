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

using System.Globalization;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.ReportView;

[TestClass]
public class FileNameToMonikerConverterTest
{
    private FileNameToMonikerConverter converter;

    [TestInitialize]
    public void Setup() => converter = new FileNameToMonikerConverter();

    [TestMethod]
    public void Convert_NullValue_ReturnsNull()
    {
        var result = converter.Convert(null, typeof(ImageMoniker), null, CultureInfo.InvariantCulture);

        result.Should().BeNull();
    }

    [TestMethod]
    public void Convert_NonStringValue_ReturnsDocumentMoniker()
    {
        var result = converter.Convert(123, typeof(ImageMoniker), null, CultureInfo.InvariantCulture);

        result.Should().BeNull();
    }

    [TestMethod]
    [DataRow("file.cs", nameof(KnownMonikers.CSFileNode))]
    [DataRow("file.vb", nameof(KnownMonikers.VBFileNode))]
    [DataRow("file.cpp", nameof(KnownMonikers.CPPFileNode))]
    [DataRow("file.c", nameof(KnownMonikers.CFile))]
    [DataRow("file.cc", nameof(KnownMonikers.CFile))]
    [DataRow("file.ccx", nameof(KnownMonikers.CFile))]
    [DataRow("file.h", nameof(KnownMonikers.CPPHeaderFile))]
    [DataRow("file.hpp", nameof(KnownMonikers.CPPHeaderFile))]
    [DataRow("file.hxx", nameof(KnownMonikers.CPPHeaderFile))]
    [DataRow("file.js", nameof(KnownMonikers.JSScript))]
    [DataRow("file.ts", nameof(KnownMonikers.TSFileNode))]
    [DataRow("file.json", nameof(KnownMonikers.JSONScript))]
    [DataRow("file.xml", nameof(KnownMonikers.XMLFile))]
    [DataRow("file.html", nameof(KnownMonikers.HTMLFile))]
    [DataRow("file.htm", nameof(KnownMonikers.HTMLFile))]
    [DataRow("file.css", nameof(KnownMonikers.StyleSheet))]
    [DataRow("file.scss", nameof(KnownMonikers.StyleSheet))]
    [DataRow("file.less", nameof(KnownMonikers.StyleSheet))]
    [DataRow("file.png", nameof(KnownMonikers.Image))]
    [DataRow("file.jpg", nameof(KnownMonikers.Image))]
    [DataRow("file.jpeg", nameof(KnownMonikers.Image))]
    [DataRow("file.gif", nameof(KnownMonikers.Image))]
    [DataRow("file.bmp", nameof(KnownMonikers.Image))]
    [DataRow("file.ico", nameof(KnownMonikers.Image))]
    [DataRow("file.svg", nameof(KnownMonikers.Image))]
    [DataRow("file.txt", nameof(KnownMonikers.TextFile))]
    [DataRow("file.md", nameof(KnownMonikers.MarkdownFile))]
    [DataRow("file.yml", nameof(KnownMonikers.ConfigurationFile))]
    [DataRow("file.yaml", nameof(KnownMonikers.ConfigurationFile))]
    [DataRow("file.config", nameof(KnownMonikers.ConfigurationFile))]
    [DataRow("file.dll", nameof(KnownMonikers.Library))]
    [DataRow("file.exe", nameof(KnownMonikers.Application))]
    public void Convert_KnownExtensions_ReturnsExpectedMoniker(string fileName, string expectedMonikerName)
    {
        var expected = typeof(KnownMonikers).GetProperty(expectedMonikerName).GetValue(null);

        var result = converter.Convert(fileName, typeof(ImageMoniker), null, CultureInfo.InvariantCulture);

        VerifyImageMonikerEquals((ImageMoniker)expected, result);
    }

    [TestMethod]
    [DataRow("file.unknown")]
    [DataRow("file.")]
    [DataRow("file")]
    public void Convert_UnknownOrNoExtension_ReturnsDocumentMoniker(string fileName)
    {
        var result = converter.Convert(fileName, typeof(ImageMoniker), null, CultureInfo.InvariantCulture);

        VerifyImageMonikerEquals(KnownMonikers.Document, result);
    }

    [TestMethod]
    public void ConvertBack_ThrowsNotImplementedException()
    {
        var act = () => converter.ConvertBack(null, null, null, CultureInfo.InvariantCulture);

        act.Should().Throw<NotImplementedException>();
    }

    private static void VerifyImageMonikerEquals(ImageMoniker expected, object actual)
    {
        actual.Should().Be(expected);
        actual.Should().BeOfType<ImageMoniker>();
        var actualMoniker = (ImageMoniker)actual;
        actualMoniker.Guid.Should().Be(expected.Guid);
        actualMoniker.Id.Should().Be(expected.Id);
    }
}
