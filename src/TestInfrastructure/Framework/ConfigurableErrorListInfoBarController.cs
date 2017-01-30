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

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    internal class ConfigurableErrorListInfoBarController : IErrorListInfoBarController
    {
        private int refreshCalled;
        private int resetCalled;

        #region IErrorListInfoBarController
        void IErrorListInfoBarController.Refresh()
        {
            this.refreshCalled++;
        }

        void IErrorListInfoBarController.Reset()
        {
            this.resetCalled++;
        }
        #endregion

        #region Test helpers
        public void AssertRefreshCalled(int expectedNumberOfTimes)
        {
            expectedNumberOfTimes.Should().Be( this.refreshCalled, $"{nameof(IErrorListInfoBarController.Refresh)} called unexpected number of times");
        }

        public void AssertResetCalled(int expectedNumberOfTimes)
        {
            expectedNumberOfTimes.Should().Be( this.resetCalled, $"{nameof(IErrorListInfoBarController.Reset)} called unexpected number of times");
        }
        #endregion
    }
}
