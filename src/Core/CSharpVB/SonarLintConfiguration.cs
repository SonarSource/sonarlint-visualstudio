/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2021 SonarSource SA
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
    /* XML-serializable classes to create a SonarLint.xml file.
        Example:

<?xml version=""1.0"" encoding=""UTF-8""?>
<AnalysisInput>

  <Settings>
    <Setting>
      <Key>sonar.cs.prop1</Key>
      <Value>value 1</Value>
    </Setting>
    <!-- more settings ... -->
  </Settings>

  <Rules>
    <Rule>
      <Key>s111</Key>
    </Rule>

    <Rule>
      <Key>s222</Key>
      <Parameters>
        <Parameter>
          <Key>param1</Key>
          <Value>param value1</Value>
        </Parameter>
        <!-- more parameters ... -->
      </Parameters>
    </Rule>
  </Rules>
</AnalysisInput>

         */

    [XmlType(TypeName = "AnalysisInput")]
    public class SonarLintConfiguration
    {
        [XmlArrayItem(ElementName = "Setting")]
        public List<SonarLintKeyValuePair> Settings { get; set; } = new List<SonarLintKeyValuePair>();

        [ XmlArrayItem(ElementName = "Rule")]
        public List<SonarLintRule> Rules { get; set; } = new List<SonarLintRule>();
    }

    public class SonarLintRule
    {
        [XmlElement]
        public string Key { get; set; }

        [XmlArrayItem(ElementName = "Parameter")]
        public List<SonarLintKeyValuePair> Parameters { get; set; } = new List<SonarLintKeyValuePair>();
    }

    // The SonarLint.xml file has Settings elements and 
    public class SonarLintKeyValuePair
    {
        [XmlElement]
        public string Key { get; set; }

        [XmlElement]
        public string Value { get; set; }
    }
}
