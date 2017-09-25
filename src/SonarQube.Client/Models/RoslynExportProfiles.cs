/*
 * SonarQube Client
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

using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace SonarQube.Client.Models
{
    public class ServerLanguage
    {
        public static readonly ServerLanguage CSharp = new ServerLanguage("cs");
        public static readonly ServerLanguage VbNet = new ServerLanguage("vbnet");

        public string Key { get; }

        private ServerLanguage(string key)
        {
            Key = key;
        }
    }

    public class RoslynExportProfileRequest
    {
        public ServerLanguage Language { get; set; }
        public string QualityProfileName { get; set; }
    }

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

    /// <summary>
    /// XML-serializable data class that contains metadata required to deploy analyzers
    /// </summary>
    public class Deployment
    {
        [XmlArray("NuGetPackages")]
        [XmlArrayItem(Type = typeof(NuGetPackageInfo), ElementName = "NuGetPackage")]
        public List<NuGetPackageInfo> NuGetPackages { get; set; }
    }

    /// <summary>
    /// XML-serializable data class for Roslyn export profile information
    /// </summary>
    [XmlRoot]
    public class RoslynExportProfile
    {
        [XmlAttribute]
        public string Version { get; set; }

        public Deployment Deployment { get; set; }

        public Configuration Configuration { get; set; }

        #region Serialization

        public static RoslynExportProfile Load(TextReader reader)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(RoslynExportProfile));
            RoslynExportProfile profile = serializer.Deserialize(reader) as RoslynExportProfile;
            return profile;
        }

        #endregion
    }
}
