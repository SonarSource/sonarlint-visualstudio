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
