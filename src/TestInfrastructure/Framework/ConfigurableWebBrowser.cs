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


using System.Collections.Generic;
using FluentAssertions;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public class ConfigurableWebBrowser : IWebBrowser
    {
        private readonly IList<string> navigatedUrls = new List<string>();

        #region Test Helpers
        public void AssertNavigateToCalls(int numCalls)
        {
            this.navigatedUrls.Should().HaveCount(numCalls, "Unexpected number of calls to NavigateTo");
        }

        public void AssertRequestToNavigateTo(string url)
        {
            navigatedUrls.Should().Contain(url, $"URL '{url}' was not navigated to");
        }

        #endregion

        #region IWebBrowser

        void IWebBrowser.NavigateTo(string url)
        {
            this.navigatedUrls.Add(url);
        }

        #endregion
    }
}
