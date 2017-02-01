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

using Microsoft.VisualStudio.TestTools.UnitTesting; using FluentAssertions;
using SonarLint.VisualStudio.Integration.Service;
using System;

namespace SonarLint.VisualStudio.Integration.UnitTests.Service
{
    [TestClass]
    public class VersionHelperTests
    {
        [TestMethod]
        public void VersionHelper_Compare_NullVersionStrings_ThrowsException()
        {
            Exceptions.Expect<ArgumentNullException>(() => VersionHelper.Compare(null, "1.2.3"));
            Exceptions.Expect<ArgumentNullException>(() => VersionHelper.Compare("1.2.3", null));
        }

        [TestMethod]
        public void VersionHelper_Compare_InvalidVersionStrings_ThrowsException()
        {
            Exceptions.Expect<ArgumentException>(() => VersionHelper.Compare("notaversion", "1.2.3"));
            Exceptions.Expect<ArgumentException>(() => VersionHelper.Compare("1.2.3", "notaversion"));
        }

        [TestMethod]
        public void VersionHelper_Compare_SameVersionString_Release_AreSame()
        {
            // Act
            int result = VersionHelper.Compare("1.2.3", "1.2.3");

            // Verify
            result.Should().Be(0);
        }

        [TestMethod]
        public void VersionHelper_Compare_SameVersionString_Prerelease_AreSame()
        {
            // Test case 1: same 'dev string'
            // Act
            int result1 = VersionHelper.Compare("1.0-rc1", "1.0-rc2");

            // Verify
            result1.Should().Be(0);
        }

        [TestMethod]
        public void VersionHelper_Compare_ReleaseAndPrerelease_ComparesOnlyNumericParts()
        {
            // Act + Verify
            (VersionHelper.Compare("1.1", "1.2-beta") < 0).Should().BeTrue();
            (VersionHelper.Compare("1.1-beta", "1.2") < 0).Should().BeTrue();
        }

        [TestMethod]
        public void VersionHelper_Compare_NextMinorVersion()
        {
            // Act + Verify
            (VersionHelper.Compare("1.2", "1.3") < 0).Should().BeTrue();
            (VersionHelper.Compare("1.3", "1.2") > 0).Should().BeTrue();
        }
    }
}
