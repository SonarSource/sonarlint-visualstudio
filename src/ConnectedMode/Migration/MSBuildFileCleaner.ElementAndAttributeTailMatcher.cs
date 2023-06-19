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
        private static class ElementAndAttributeTailMatcher
        {
            /// <summary>
            /// Returns a list of nodes in the document matching the following criteria:
            ///   <ElementName AttributeName="xxxx[value to tail match] ... />
            /// e.g.
            ///   <Include Path="..\..\.sonarlint\my-project-keyvb.ruleset" Action="Default" />
            /// </summary>
            public static IList<XmlNode> Find(XmlDocument document, string elementName, string attributeName,
                params string[] valuesToTailMatch)
            {
                var nodesToRemove = new List<XmlNode>();
                foreach (XmlNode item in document.GetElementsByTagName(elementName))
                {
                    if (ContainsAttributeWithValue(item.Attributes, attributeName, valuesToTailMatch))
                    {
                        nodesToRemove.Add(item);
                    }
                }
                return nodesToRemove;
            }

            private static bool ContainsAttributeWithValue(XmlAttributeCollection attributeCollection,
                string attributeName,
                params string[] partialRulesetPaths)
            {
                return attributeCollection
                            .Cast<XmlAttribute>()
                            .Any(attribute =>
                                partialRulesetPaths.Any(path =>
                                    attribute.Name == attributeName &&
                                    attribute.Value.EndsWith(path, System.StringComparison.OrdinalIgnoreCase)));
            }
        }
    }
}
