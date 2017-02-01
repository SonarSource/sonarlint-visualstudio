/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA and Microsoft Corporation
 * mailto: contact AT sonarsource DOT com
 *
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
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