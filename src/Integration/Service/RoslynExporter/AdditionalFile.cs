/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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

using System.Xml;
using System.Xml.Serialization;

namespace SonarLint.VisualStudio.Integration.Service
{
    /// <summary>
    /// XML-serializable data class for a single analyzer AdditionalFile
    /// i.e. the mechanism used by Roslyn to pass additional data to analyzers.
    /// </summary>
    public class AdditionalFile
    {
        /// <summary>
        /// The name of the file the content should be saved to
        /// </summary>
        [XmlAttribute]
        public string FileName { get; set; }

        /// <summary>
        /// The content of the file
        /// </summary>
        [XmlText(DataType = "base64Binary")]
        public byte[] Content { get; set; }
    }
}