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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Linq;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class LanguageTests
    {
        [TestMethod]
        public void Language_Ctor_Guid_ArgChecks()
        {
            // Setup
            var key = "k";
            var name = "MyName";
            var guid = Guid.NewGuid();

            // Act + Verify
            // Nulls
            Exceptions.Expect<ArgumentNullException>(() => new Language(name, null, guid));
            Exceptions.Expect<ArgumentNullException>(() => new Language(null, key, guid));
        }

        [TestMethod]
        public void Language_Ctor_GuidString_ArgChecks()
        {
            // Setup
            var key = "k";
            var name = "MyName";
            var guidString = Guid.NewGuid().ToString("N");

            // Act + Verify
            // Nulls
            Exceptions.Expect<ArgumentNullException>(() => new Language(name, null, guidString));
            Exceptions.Expect<ArgumentNullException>(() => new Language(null, key, guidString));
            Exceptions.Expect<ArgumentNullException>(() => new Language(name, key, (string)null));

            // Bad GUID
            Exceptions.Expect<FormatException>(() => new Language(name, key, "thisisnotaguid"));
        }

        [TestMethod]
        public void Language_IsSupported_SupportedLanguage_IsTrue()
        {
            // Act + Verify
            foreach(var supportedLang in Language.SupportedLanguages)
            {
                Assert.IsTrue(supportedLang.IsSupported, "Supported language should be supported");
            }
        }

        [TestMethod]
        public void Language_ForProject_KnownLanguage_ArgChecks()
        {
            Exceptions.Expect<ArgumentNullException>(() => Language.ForProject(null));
        }

        [TestMethod]
        public void Language_ForProject_KnownLanguage_ReturnsCorrectLanguage()
        {
            // Test case 1: Unknown
            // Setup
            var otherProject = new ProjectMock("other.proj");

            // Act
            var otherProjectLanguage = Language.ForProject(otherProject);

            // Verify
            Assert.AreEqual(Language.Unknown, otherProjectLanguage, "Unexpected Language for unknown project");

            // Test case 2: C#
            // Setup
            var csProject = new ProjectMock("cs1.csproj");
            csProject.SetCSProjectKind();

            // Act
            var csProjectLanguage = Language.ForProject(csProject);

            // Verify
            Assert.AreEqual(Language.CSharp, csProjectLanguage, "Unexpected Language for C# project");

            // Test case 3: VB
            // Setup
            var vbNetProject = new ProjectMock("vb1.vbproj");
            vbNetProject.SetVBProjectKind();

            // Act
            var vbNetProjectLanguage = Language.ForProject(vbNetProject);

            // Verify
            Assert.AreEqual(Language.VBNET, vbNetProjectLanguage, "Unexpected Language for C# project");
        }

        [TestMethod]
        public void Language_Equality()
        {
            // Setup
            var lang1a = new Language("Language 1", "lang1", "{4FE75C7D-F43F-4A72-940C-47C97710BCCA}");
            var lang1b = new Language("Language 1", "lang1", "{4FE75C7D-F43F-4A72-940C-47C97710BCCA}");
            var lang2 = new Language("Language 2", "lang2", "{7A128822-05AA-49D0-A3C7-16F03F3A92E5}");

            // Act + Verify
            Assert.AreEqual(lang1a, lang1b, "Languages with the same keys and GUIDs should be equal");
            Assert.AreNotEqual(lang1a, lang2, "Languages with different keys and GUIDs should NOT be equal");
        }
    }
}
