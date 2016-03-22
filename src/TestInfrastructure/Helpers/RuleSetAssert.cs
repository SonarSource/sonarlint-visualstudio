//-----------------------------------------------------------------------
// <copyright file="RuleSetAssert.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Xml.Linq;

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

            Assert.IsTrue(contentEqual, contentMessage);
            Assert.IsTrue(declarationEqual, declarationMessage);
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
