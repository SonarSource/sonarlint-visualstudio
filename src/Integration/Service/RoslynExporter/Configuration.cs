//-----------------------------------------------------------------------
// <copyright file="Configuration.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;

namespace SonarLint.VisualStudio.Integration.Service
{
    /// <summary>
    /// XML-serializable data class for Roslyn export configuration element
    /// </summary>
    public class Configuration
    {
        [XmlAnyElement("RuleSet")]
        public XmlElement RuleSet { get; set; }

        [XmlArray("AdditionalFiles")]
        [XmlArrayItem("AdditionalFile")]
        public List<AdditionalFile> AdditionalFiles { get; set; }
    }
}
