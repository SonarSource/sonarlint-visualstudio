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

using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace SonarLint.VisualStudio.ConnectedMode.Migration
{
    internal partial class MSBuildFileCleaner
    {
        private static class AdditionalFilesFinder
        {
            public static IList<XmlNode> Find(XmlDocument document, LegacySettings legacySettings)
            {
                const string AdditionalFilesElementName = "AdditionalFiles";

                var nodesToRemove = new List<XmlNode>();
                foreach (XmlNode item in document.GetElementsByTagName(AdditionalFilesElementName))
                {
                    if (ContainsSonarLintXmlReferenceInAttributes(item.Attributes,
                            legacySettings.PartialVBSonarLintXmlPath,
                            legacySettings.PartialCSharpSonarLintXmlPath))
                    {
                        nodesToRemove.Add(item);
                    }
                }
                return nodesToRemove;
            }

            private static bool ContainsSonarLintXmlReferenceInAttributes(XmlAttributeCollection attributeCollection,
                params string[] sonarlintXmlPaths)
            {
                // Matches the following:
                // 		<AdditionalFiles Include="..\..\.sonarlint\my_project_key\CSharp\SonarLint.xml" />
                return attributeCollection
                    .Cast<XmlAttribute>()
                    .Any(attribute =>
                        sonarlintXmlPaths.Any(path =>
                            attribute.Name == "Include" &&
                            attribute.Value.EndsWith(path, System.StringComparison.OrdinalIgnoreCase)));
            }
        }
    }
}
