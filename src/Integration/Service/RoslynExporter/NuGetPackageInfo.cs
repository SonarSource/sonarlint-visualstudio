//-----------------------------------------------------------------------
// <copyright file="NuGetPackageInfo.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using System.Xml.Serialization;

namespace SonarLint.VisualStudio.Integration.Service
{
    /// <summary>
    /// XML-serializable data class for a single NuGet package containing an analyzer
    /// </summary>
    public class NuGetPackageInfo
    {
        [XmlAttribute("Id")]
        public string Id { get; set; }

        [XmlAttribute("Version")]
        public string Version { get; set; }
    }
}
