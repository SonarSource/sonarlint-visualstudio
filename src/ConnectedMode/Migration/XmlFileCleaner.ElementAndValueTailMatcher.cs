﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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
    internal partial class XmlFileCleaner
    {
        private static class ElementAndValueTailMatcher
        {
            /// <summary>
            /// Returns a list of nodes in the document matching the following criteria:
            ///   <ElementName ... ></ElementName>"xxxx[value to tail match]</ElementName>
            /// e.g.
            ///   <CodeAnalysisRuleSet>..\..\.sonarlint\project_key_aaacsharp.ruleset</CodeAnalysisRuleSet>
            /// </summary>
            public static IList<XmlNode> Find(XmlDocument document, string elementName,
                params string[] valuesToTailMatch)
            {
                var nodesToRemove = new List<XmlNode>();
                foreach (XmlNode item in document.GetElementsByTagName(elementName))
                {
                    if (HasTextValue(item, valuesToTailMatch))
                    {
                        nodesToRemove.Add(item);
                    }
                }
                return nodesToRemove;
            }

            private static bool HasTextValue(XmlNode item, params string[] valuesToTailMatch)
            {
                var innerText = item.InnerText;
                return valuesToTailMatch
                    .Any(val => innerText.EndsWith(val, System.StringComparison.OrdinalIgnoreCase));
            }
        }
    }
}
