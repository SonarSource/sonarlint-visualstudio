/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace SonarLint.VisualStudio.Core.CSharpVB
{
    /// <summary>
    /// Helper class to serialize objects to and from XML
    /// </summary>
    public static class Serializer
    {
        /// <summary>
        /// Return the object as an XML string
        /// </summary>
        public static string ToString<T>(T model) where T : class
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            using (var stream = new MemoryStream())
            using (var writer = new StreamWriter(stream))
            {
                Write(model, writer);
                var data = stream.ToArray();
                return Encoding.UTF8.GetString(data);
            }
        }

        private static void Write<T>(T model, TextWriter writer) where T : class
        {
            var serializer = new XmlSerializer(typeof(T));

            var settings = new XmlWriterSettings
            {
                CloseOutput = true,
                ConformanceLevel = ConformanceLevel.Document,
                Indent = true,
                NamespaceHandling = NamespaceHandling.OmitDuplicates,
                OmitXmlDeclaration = false,
                Encoding = Encoding.UTF8
            };

            using (var xmlWriter = XmlWriter.Create(writer, settings))
            {
                serializer.Serialize(xmlWriter, model);
                xmlWriter.Flush();
            }
        }
    }
}
