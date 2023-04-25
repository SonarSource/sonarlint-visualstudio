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
using System.IO;
using System.IO.Abstractions;
using System.Text;
using System.Xml;

namespace SonarLint.VisualStudio.ConnectedMode.Binding
{
    internal class ImportBeforeFileGenerator
    {
        private readonly IFileSystem fileSystem;

        private const string targetsFileName = "SonarLint.targets";

        public ImportBeforeFileGenerator() : this(new FileSystem()) { }

        public ImportBeforeFileGenerator(IFileSystem fileSystem)
        {
            this.fileSystem = fileSystem;

            CreateTargetsFileIfNotExists();
        }

        private void CreateTargetsFileIfNotExists()
        {
            var sb = new StringBuilder();
            var xmlWriter = Create(sb);

            xmlWriter.WriteStartElement("Project", "http://schemas.microsoft.com/developer/msbuild/2003");
            xmlWriter.WriteAttributeString("xmlns", "http://schemas.microsoft.com/developer/msbuild/2003");

            xmlWriter.WriteStartElement("ItemGroup");

            xmlWriter.WriteEndElement();
            xmlWriter.WriteEndElement();

            xmlWriter.Close();
            WriteFile(sb.ToString());
        }

        private void WriteFile(string xml)
        {
            var pathToImportBefore = GetPathToImportBefore();

            if (!fileSystem.Directory.Exists(pathToImportBefore))
            {
                fileSystem.Directory.CreateDirectory(pathToImportBefore);
            }

            var fullPath = Path.Combine(pathToImportBefore, targetsFileName);

            if (fileSystem.File.Exists(fullPath) && fileSystem.File.ReadAllText(fullPath) == xml)
            {
                return;
            }

            fileSystem.File.WriteAllText(fullPath, xml);
        }

        private XmlWriter Create(StringBuilder sb)
        {
            var stringWriter = new StringWriter(sb);

            var settings = new XmlWriterSettings
            {
                ConformanceLevel = ConformanceLevel.Fragment,
                Encoding = Encoding.UTF8,
                OmitXmlDeclaration = true,
                Indent = true,
                CloseOutput = true,
                WriteEndDocumentOnClose = true
            };

            return XmlWriter.Create(stringWriter, settings);
        }

        private string GetPathToImportBefore()
        {
            var localAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA");
            var pathToImportBefore = Path.Combine(localAppData, "Microsoft", "MSBuild", "Current", "Microsoft.Common.targets", "ImportBefore");

            return pathToImportBefore;
        }
    }
}
