/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Xml;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.ConnectedMode.Migration
{
    [Export(typeof(IFileCleaner))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    internal partial class XmlFileCleaner : IFileCleaner
    {
        /// <summary>
        /// Return value indicating the file has not changed
        /// </summary>
        public const string Unchanged = null;

        private readonly ILogger logger;
        private readonly IXmlDocumentHelper xmlDocumentHelper;

        [ImportingConstructor]
        public XmlFileCleaner(ILogger logger) : this(logger, new XmlDocumentHelper())
        {
        }

        internal /* for testing */ XmlFileCleaner(ILogger logger, IXmlDocumentHelper xmlDocumentHelper)
        {
            this.logger = logger;
            this.xmlDocumentHelper = xmlDocumentHelper;
        }

        public string Clean(string content, LegacySettings legacySettings, CancellationToken token)
        {
            if (!xmlDocumentHelper.TryLoadFromString(content, out var document))
            {
                logger.WriteLine(MigrationStrings.Cleaner_NotXml);
                return Unchanged;
            }

            var nodesToRemove = new List<XmlNode>();

            nodesToRemove.AddRange(FindAdditionalFiles(document, legacySettings));
            nodesToRemove.AddRange(FindNoneRulesets(document, legacySettings));
            nodesToRemove.AddRange(FindIncludedRulesets(document, legacySettings));
            nodesToRemove.AddRange(FindRulesetProperties(document, legacySettings));

            if (!nodesToRemove.Any())
            {
                logger.WriteLine(MigrationStrings.Cleaner_Unchanged);
                return Unchanged;
            }

            logger.WriteLine(MigrationStrings.Cleaner_RemovingSettings, nodesToRemove.Count);

            nodesToRemove.ForEach(node =>
            {
                Debug.Assert(node.ParentNode != null);

                // If the only child elements in the parent element are white spaces and the node the whole parent elemnt can be removed.
                var nodeToRemove = ShouldRemoveParentElement(node) ? node.ParentNode : node;

                // This will ensure that there is no white space left behind when removing the actual currentNode.
                if (nodeToRemove.PreviousSibling.NodeType == XmlNodeType.Whitespace)
                {
                    nodeToRemove.ParentNode.RemoveChild(nodeToRemove.PreviousSibling);
                }

                nodeToRemove.ParentNode.RemoveChild(nodeToRemove);
            });

            return xmlDocumentHelper.SaveToString(document);
        }

        private static bool ShouldRemoveParentElement(XmlNode node)
        {
            foreach (XmlNode child in node.ParentNode.ChildNodes)
            {
                if (child.NodeType != XmlNodeType.Whitespace && child != node)
                {
                    return false;
                }
            }

            return node.ParentNode.ParentNode.ParentNode != null;
        }

        private static IList<XmlNode> FindAdditionalFiles(XmlDocument document, LegacySettings legacySettings)
            => ElementAndAttributeTailMatcher.Find(document, "AdditionalFiles", "Include",
                legacySettings.PartialCSharpSonarLintXmlPath,
                legacySettings.PartialVBSonarLintXmlPath);

        // e.g. <None Include="..\SolutionFolder\.sonarlint\myprojectkeyvb.ruleset">
        private static IList<XmlNode> FindNoneRulesets(XmlDocument document, LegacySettings legacySettings)
            => ElementAndAttributeTailMatcher.Find(document, "None", "Include",
                legacySettings.PartialCSharpRuleSetPath,
                legacySettings.PartialVBRuleSetPath);

        private static IList<XmlNode> FindIncludedRulesets(XmlDocument document, LegacySettings legacySettings)
            => ElementAndAttributeTailMatcher.Find(document, "Include", "Path",
                legacySettings.PartialCSharpRuleSetPath,
                legacySettings.PartialVBRuleSetPath);

        private IEnumerable<XmlNode> FindRulesetProperties(XmlDocument document, LegacySettings legacySettings)
            => ElementAndValueTailMatcher.Find(document, "CodeAnalysisRuleSet",
                legacySettings.PartialCSharpRuleSetPath,
                legacySettings.PartialVBRuleSetPath);
    }
}
