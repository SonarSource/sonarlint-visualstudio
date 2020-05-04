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

using System.Collections.Generic;
using System.Xml.Serialization;

namespace SonarLint.VisualStudio.Core.CSharpVB
{
    // XML data classes to serialize a RuleSet in VS format

    // Data classes copied from the SonarScanner for MSBuild and merged into a single file.
    // See https://github.com/SonarSource/sonar-scanner-msbuild/blob/5c23a7da9171e90a1970a31507dce3da3e8ee094/src/SonarScanner.MSBuild.Common/AnalysisConfig/RuleSet.cs#L27
    // and the other data files in the same folder.
    public class RuleSet
    {
        [XmlAttribute]
        public string Name { get; set; }

        [XmlAttribute]
        public string Description { get; set; }

        [XmlAttribute]
        public string ToolsVersion { get; set; }

        [XmlElement(ElementName = "Include")]
        public List<Include> Includes { get; set; }

        [XmlElement]
        public List<Rules> Rules { get; set; } = new List<Rules>();

        public string ToXml()
        {
            return Serializer.ToString(this);
        }
    }

    public class Include
    {
        [XmlAttribute]
        public string Path { get; set; }

        [XmlAttribute]
        public string Action { get; set; }
    }

    public class Rule
    {
        public Rule()
        {
        }

        public Rule(string id, string action)
        {
            Id = id;
            Action = action;
        }

        [XmlAttribute]
        public string Id { get; set; }

        [XmlAttribute]
        public string Action { get; set; }
    }

    public class Rules
    {
        [XmlAttribute]
        public string AnalyzerId { get; set; }

        [XmlAttribute]
        public string RuleNamespace { get; set; }

        [XmlElement("Rule")]
        public List<Rule> RuleList { get; set; }
    }
}
