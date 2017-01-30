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


using Xunit;
using SonarLint.VisualStudio.Integration.Service;
using System;
using FluentAssertions;

namespace SonarLint.VisualStudio.Integration.UnitTests.Service
{
    public class VersionHelperTests
    {
        [Fact]
        public void Compare_WithNullLeftVersion_ThrowsArgumentNullException()
        {
            // Arrange + Act
            Action act = () => VersionHelper.Compare(null, "1.2.3");

            // Assert
            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Compare_WithNullRightVersion_ThrowsArgumentNullException()
        {
            // Arrange + Act
            Action act = () => VersionHelper.Compare("1.2.3", null);

            // Assert
            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Compare_WithInvalidLeftVersion_ThrowsArgumentNullException()
        {
            // Arrange + Act
            Action act = () => VersionHelper.Compare("notaversion", "1.2.3");

            // Assert
            act.ShouldThrow<ArgumentException>();
        }

        [Fact]
        public void Compare_WithInvalidRightVersion_ThrowsArgumentNullException()
        {
            // Arrange + Act
            Action act = () => VersionHelper.Compare("1.2.3", "notaversion");

            // Assert
            act.ShouldThrow<ArgumentException>();
        }

        [Fact]
        public void VersionHelper_Compare_SameVersionString_Release_AreSame()
        {
            // Act
            int result = VersionHelper.Compare("1.2.3", "1.2.3");

            // Assert
            result.Should().Be(0);
        }

        [Fact]
        public void VersionHelper_Compare_SameVersionString_Prerelease_AreSame()
        {
            // Test case 1: same 'dev string'
            // Act
            int result1 = VersionHelper.Compare("1.0-rc1", "1.0-rc2");

            // Assert
            result1.Should().Be(0);
        }

        [Fact]
        public void VersionHelper_Compare_ReleaseAndPrerelease_ComparesOnlyNumericParts()
        {
            // Act + Assert
            VersionHelper.Compare("1.1", "1.2-beta")
                .Should().BeLessThan(0);
            VersionHelper.Compare("1.1-beta", "1.2")
                .Should().BeLessThan(0);
        }

        [Fact]
        public void VersionHelper_Compare_NextMinorVersion()
        {
            // Act + Assert
            VersionHelper.Compare("1.2", "1.3")
                .Should().BeLessThan(0);
            VersionHelper.Compare("1.3", "1.2")
                .Should().BeGreaterThan(0);
        }
    }
}
