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

using System.IO;
using System.Xml;

namespace SonarLint.VisualStudio.ConnectedMode.Migration
{
    /// <summary>
    /// Helper methods for working with <see cref="XmlDocument"/>
    /// </summary>
    internal interface IXmlDocumentHelper
    {
        /// <summary>
        /// Loads <see cref="XmlDocument"/> from <see cref="string"/> with preserved original formatting
        /// </summary>
        /// <param name="content">XML document contents</param>
        /// <returns><see cref="XmlDocument"/> object representation</returns>
        XmlDocument LoadFromString(string content);
        
        /// <summary>
        /// Saves <see cref="XmlDocument"/> to <see cref="string"/>
        /// </summary>
        /// <remarks>Preserves formatting and does not force add ?xml? header if the original XML didn't have it</remarks>
        /// <param name="document"><see cref="XmlDocument"/> object</param>
        /// <returns>XML document as string</returns>
        string SaveToString(XmlDocument document);
    }
    
    internal class XmlDocumentHelper : IXmlDocumentHelper
    {
        public XmlDocument LoadFromString(string content)
        {
            var xmlDocument = new XmlDocument { PreserveWhitespace = true };
            xmlDocument.LoadXml(content);
            return xmlDocument;
        }

        public string SaveToString(XmlDocument document)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var streamReader = new StreamReader(memoryStream))
                {
                    // using StringWriter introduced unexpected <?xml?> header for files that don't have it
                    document.Save(memoryStream);
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    return streamReader.ReadToEnd();
                }
            }
        }
    }
}
