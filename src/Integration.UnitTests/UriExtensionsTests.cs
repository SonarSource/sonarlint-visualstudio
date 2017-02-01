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
using System;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class UriExtensionsTests
    {
        [TestMethod]
        public void UriExtensions_NullArgChecks()
        {
            Exceptions.Expect<ArgumentNullException>(() => UriExtensions.EnsureTrailingSlash(null));
        }

        [TestMethod]
        public void UriExtensions_EnsureTrailingSlash_NoTrailingSlash_AppendsSlash()
        {
            // Act
            var noSlashResult = UriExtensions.EnsureTrailingSlash(new Uri("http://localhost/NoSlash"));

            // Verify
            noSlashResult.ToString().Should().Be("http://localhost/NoSlash/", "Unexpected normalization of URI without trailing slash");
        }

        [TestMethod]
        public void UriExtensions_EnsureTrailingSlash_HasTrailingSlash_ReturnsSameInstance()
        {
            // Act
            var withSlashResult = UriExtensions.EnsureTrailingSlash(new Uri("http://localhost/WithSlash/"));

            // Verify
            withSlashResult.ToString().Should().Be("http://localhost/WithSlash/", "Unexpected normalization of URI already with trailing slash");
        }
    }
}
