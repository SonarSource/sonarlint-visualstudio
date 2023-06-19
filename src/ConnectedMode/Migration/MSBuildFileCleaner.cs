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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Xml;
using SonarLint.VisualStudio.Integration;

namespace SonarLint.VisualStudio.ConnectedMode.Migration
{
    [Export(typeof(IFileCleaner))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    internal class MSBuildFileCleaner : IFileCleaner
    {
        /// <summary>
        /// Return value indicating the file has not changed
        /// </summary>
        public const string Unchanged = null;

        private readonly ILogger logger;
        private readonly IXmlDocumentHelper xmlDocumentHelper;

        [ImportingConstructor]
        public MSBuildFileCleaner(ILogger logger) : this(logger, new XmlDocumentHelper())
        {
        }

        internal /* for testing */ MSBuildFileCleaner(ILogger logger, IXmlDocumentHelper xmlDocumentHelper)
        {
            this.logger = logger;
            this.xmlDocumentHelper = xmlDocumentHelper;
        }

        public string Clean(string content, LegacySettings legacySettings, CancellationToken token)
        {
            var document = xmlDocumentHelper.LoadFromString(content);

            var nodesToRemove = new List<XmlNode>();

            nodesToRemove.AddRange(GetAdditionalFilesToRemove(document, legacySettings));
            nodesToRemove.AddRange(GetRulesetIncludesToRemove(document, legacySettings));

            if (!nodesToRemove.Any())
            {
                logger.LogVerbose("No settings to remove");
                return Unchanged;
            }

            logger.WriteLine(MigrationStrings.Cleaner_RemovingSettings, nodesToRemove.Count);

            nodesToRemove.ForEach(node =>
            {
                Debug.Assert(node.ParentNode != null);
                node.ParentNode.RemoveChild(node);
            });

            return xmlDocumentHelper.SaveToString(document);
        }

        private IList<XmlNode> GetAdditionalFilesToRemove(XmlDocument document, LegacySettings legacySettings)
        {
            const string AdditionalFilesElementName = "AdditionalFiles";

            var nodesToRemove = new List<XmlNode>();
            foreach (XmlNode item in document.GetElementsByTagName(AdditionalFilesElementName))
            {
                if (ContainsSonarLintXmlReferenceInAttributes(item.Attributes,
                        legacySettings.PartialVBSonarLintXmlPath,
                        legacySettings.PartialCSharpSonarLintXmlPath))
                {
                    LogVerbose("Detected SonarLint.xml: " + item.Value);
                    nodesToRemove.Add(item);
                }
            }
            return nodesToRemove;
        }

        private IList<XmlNode> GetRulesetIncludesToRemove(XmlDocument document, LegacySettings legacySettings)
        {
            const string IncludeElementName = "Include";

            var nodesToRemove = new List<XmlNode>();
            foreach (XmlNode item in document.GetElementsByTagName(IncludeElementName))
            {
                if (ContainsGeneratedRulesetReferenceInAttributes(item.Attributes,
                        legacySettings.PartialCSharpRuleSetPath,
                        legacySettings.PartialVBRuleSetPath))
                {
                    LogVerbose("Detected reference to generated ruleset: " + item.Value);
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

        private static bool ContainsGeneratedRulesetReferenceInAttributes(XmlAttributeCollection attributeCollection,
            params string[] sonarlintXmlPaths)
        {
            // Matches the following:
            //   <Include Path="..\..\.sonarlint\duncanp_junkvb.ruleset" Action="Default" />
            return attributeCollection
                        .Cast<XmlAttribute>()
                        .Any(attribute =>
                            sonarlintXmlPaths.Any(path =>
                                attribute.Name == "Path" &&
                                attribute.Value.EndsWith(path, System.StringComparison.OrdinalIgnoreCase)));
        }

        private void LogVerbose(string message) => logger.LogVerbose("[Migration] " + message);
    }
}
