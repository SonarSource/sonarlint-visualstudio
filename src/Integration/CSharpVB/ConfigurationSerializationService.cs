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

using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using SonarLint.VisualStudio.Core.CSharpVB;

namespace SonarLint.VisualStudio.Integration.CSharpVB;

public static class ConfigurationSerializationService
{
    /// <summary>
    /// The file will never be written to disk so the path is irrelevant.
    /// It only needs to be named 'SonarLint.Xml' so the sonar-dotnet analyzers could load it.
    /// </summary>
    private static readonly string DummySonarLintXmlFilePath = Path.Combine(Path.GetTempPath(), "SonarLint.xml");

    public static AdditionalText Convert(string sonarLintXmlFileContent)
    {
        var sonarLintXmlAdditionalText = new AdditionalTextImpl(DummySonarLintXmlFilePath, sonarLintXmlFileContent);

        return sonarLintXmlAdditionalText;
    }

    public static string Serialize(SonarLintConfiguration sonarLintConfiguration)
    {
        var settings = new XmlWriterSettings
        {
            CloseOutput = true,
            ConformanceLevel = ConformanceLevel.Document,
            Indent = true,
            NamespaceHandling = NamespaceHandling.OmitDuplicates,
            OmitXmlDeclaration = false,
            Encoding = new UTF8Encoding(false) // to avoid generating unicode BOM
        };

        using var stream = new MemoryStream { Position = 0 };
        using var xmlWriter = XmlWriter.Create(stream, settings);

        var serializer = new XmlSerializer(typeof(SonarLintConfiguration));
        serializer.Serialize(xmlWriter, sonarLintConfiguration);
        xmlWriter.Flush();

        var data = stream.ToArray();
        var sonarLintXmlFileContent = Encoding.UTF8.GetString(data);

        return sonarLintXmlFileContent;
    }

    // There isn't a public implementation of source text so we need to create one
    internal class AdditionalTextImpl : AdditionalText
    {
        private readonly SourceText sourceText;

        public override string Path { get; }

        public AdditionalTextImpl(string path, string content)
        {
            Path = path;
            sourceText = SourceText.From(content);
        }

        public override SourceText GetText(CancellationToken cancellationToken = default) => sourceText;
    }
}
