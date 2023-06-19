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
        private static class IncludedRulesetFinder
        {
            public static IList<XmlNode> Find(XmlDocument document, LegacySettings legacySettings)
            {
                const string IncludeElementName = "Include";

                var nodesToRemove = new List<XmlNode>();
                foreach (XmlNode item in document.GetElementsByTagName(IncludeElementName))
                {
                    if (ContainsGeneratedRulesetReferenceInAttributes(item.Attributes,
                            legacySettings.PartialCSharpRuleSetPath,
                            legacySettings.PartialVBRuleSetPath))
                    {
                        nodesToRemove.Add(item);
                    }
                }
                return nodesToRemove;
            }

            private static bool ContainsGeneratedRulesetReferenceInAttributes(XmlAttributeCollection attributeCollection,
                params string[] partialRulesetPaths)
            {
                // Matches the following:
                //   <Include Path="..\..\.sonarlint\my-project-keyvb.ruleset" Action="Default" />
                return attributeCollection
                            .Cast<XmlAttribute>()
                            .Any(attribute =>
                                partialRulesetPaths.Any(path =>
                                    attribute.Name == "Path" &&
                                    attribute.Value.EndsWith(path, System.StringComparison.OrdinalIgnoreCase)));
            }
        }
    }
}
