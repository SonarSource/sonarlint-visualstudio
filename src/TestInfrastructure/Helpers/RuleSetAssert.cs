/*
 * SonarLint for Visual Studio
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

using System;
using System.IO;
using System.Xml.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.CodeAnalysis.RuleSets;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    internal static class RuleSetAssert
    {
        public static void AreEqual(RuleSet expected, RuleSet actual, string message = null)
        {
            // Load files
            XDocument expectedDocument = expected.ToXDocument();
            XDocument actualDocument = actual.ToXDocument();

            string nl = Environment.NewLine;

            string contentMessage = (message ?? string.Empty) + $"Documents are not equal. Expected:{nl}{expectedDocument}{nl}but was:{nl}{actualDocument}";
            string declarationMessage = (message ?? string.Empty) + $"Declarations are not equal. Expected: {expectedDocument.Declaration} but was {actualDocument.Declaration}";

            bool contentEqual = XNode.DeepEquals(expectedDocument, actualDocument);
            bool declarationEqual = string.Equals(expectedDocument.Declaration.Encoding, actualDocument.Declaration.Encoding) &&
                string.Equals(expectedDocument.Declaration.Standalone, actualDocument.Declaration.Standalone) &&
                string.Equals(expectedDocument.Declaration.Version, actualDocument.Declaration.Version);

            contentEqual.Should().BeTrue(contentMessage);
            declarationEqual.Should().BeTrue(declarationMessage);
        }

        private static XDocument ToXDocument(this RuleSet ruleSet)
        {
            var tempFile = Path.GetTempFileName();
            ruleSet.WriteToFile(tempFile);

            XDocument document = XDocument.Load(tempFile);

            File.Delete(tempFile);

            return document;
        }
    }
}