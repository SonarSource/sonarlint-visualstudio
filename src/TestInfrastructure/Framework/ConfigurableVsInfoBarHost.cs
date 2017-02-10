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
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.Shell.Interop;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public class ConfigurableVsInfoBarHost : IVsInfoBarHost
    {
        private readonly List<IVsUIElement> elements = new List<IVsUIElement>();

        #region IVsInfoBarHost

        void IVsInfoBarHost.AddInfoBar(IVsUIElement uiElement)
        {
            this.elements.Contains(uiElement).Should().BeFalse();
            this.elements.Add(uiElement);
        }

        void IVsInfoBarHost.RemoveInfoBar(IVsUIElement uiElement)
        {
            this.elements.Contains(uiElement).Should().BeTrue();
            this.elements.Remove(uiElement);
        }

        #endregion IVsInfoBarHost

        #region Test helpers

        public void AssertInfoBars(int expectedNumberOfInfoBars)
        {
            this.elements.Should().HaveCount(expectedNumberOfInfoBars, "Unexpected number of info bars");
        }

        public IEnumerable<ConfigurableVsInfoBarUIElement> MockedElements
        {
            get
            {
                return this.elements.OfType<ConfigurableVsInfoBarUIElement>();
            }
        }

        #endregion Test helpers
    }
}