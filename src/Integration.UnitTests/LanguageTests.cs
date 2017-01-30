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

using FluentAssertions;

using Xunit;
using System;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public class LanguageTests
    {
        [Fact]
        public void Ctor_WithNullId_ThrowsArgumentNullException()
        {
            // Arrange
            var name = "MyName";
            var guid = Guid.NewGuid();

            // Act
            Action act = () => new Language(null, name, guid);

            // Assert
            act.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("id");
        }

        [Fact]
        public void Ctor_WithNullName_ThrowsArgumentNullException()
        {
            // Arrange
            var key = "k";
            var guid = Guid.NewGuid();

            // Act
            Action act = () => new Language(key, null, guid);

            // Assert
            act.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("name");
        }

        [Fact]
        public void Ctor_WithInvalidGuid_ThrowsFormatException()
        {
            // Arrange
            var key = "k";
            var name = "MyName";

            // Act
            Action act = () => new Language(name, key, "thisisnotaguid");

            // Assert
            act.ShouldThrow<FormatException>();
        }

        [Fact]
        public void Language_IsSupported_SupportedLanguage_IsTrue()
        {
            // Act + Assert
            foreach(var supportedLang in Language.SupportedLanguages)
            {
                supportedLang.IsSupported.Should().BeTrue( "Supported language should be supported");
            }
        }

        [Fact]
        public void ForProject_WithNullProject_ThrowsArgumentNullException()
        {
            // Arrange + Act
            Action act = () => Language.ForProject(null);

            // Assert
            act.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("dteProject");
        }

        [Fact]
        public void Language_ForProject_KnownLanguage_ReturnsCorrectLanguage()
        {
            // Test case 1: Unknown
            // Arrange
            var otherProject = new ProjectMock("other.proj");

            // Act
            var otherProjectLanguage = Language.ForProject(otherProject);

            // Assert
            Language.Unknown.Should().Be( otherProjectLanguage, "Unexpected Language for unknown project");

            // Test case 2: C#
            // Arrange
            var csProject = new ProjectMock("cs1.csproj");
            csProject.SetCSProjectKind();

            // Act
            var csProjectLanguage = Language.ForProject(csProject);

            // Assert
            Language.CSharp.Should().Be( csProjectLanguage, "Unexpected Language for C# project");

            // Test case 3: VB
            // Arrange
            var vbNetProject = new ProjectMock("vb1.vbproj");
            vbNetProject.SetVBProjectKind();

            // Act
            var vbNetProjectLanguage = Language.ForProject(vbNetProject);

            // Assert
            Language.VBNET.Should().Be( vbNetProjectLanguage, "Unexpected Language for C# project");
        }

        [Fact]
        public void Language_Equality()
        {
            // Arrange
            var lang1a = new Language("Language 1", "lang1", "{4FE75C7D-F43F-4A72-940C-47C97710BCCA}");
            var lang1b = new Language("Language 1", "lang1", "{4FE75C7D-F43F-4A72-940C-47C97710BCCA}");
            var lang2 = new Language("Language 2", "lang2", "{7A128822-05AA-49D0-A3C7-16F03F3A92E5}");

            // Act + Assert
            lang1a.Should().Be(lang1b, "Languages with the same keys and GUIDs should be equal");
            lang1a.Should().NotBe(lang2, "Languages with different keys and GUIDs should NOT be equal");
        }
    }
}
