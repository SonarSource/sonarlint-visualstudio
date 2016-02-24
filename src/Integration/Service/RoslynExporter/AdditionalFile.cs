//-----------------------------------------------------------------------
// <copyright file="AdditionalFile.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

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