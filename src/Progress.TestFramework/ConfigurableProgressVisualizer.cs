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

using SonarLint.VisualStudio.Progress.Observation;
using SonarLint.VisualStudio.Progress.Observation.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SonarLint.VisualStudio.Progress.UnitTests
{
    /// <summary>
    /// Test implementation of <see cref="IProgressVisualizer"/>
    /// </summary>
    public partial class ConfigurableProgressVisualizer : IProgressVisualizer
    {
        private bool isShown;
        private ProgressControllerViewModel viewModel;

        public ConfigurableProgressVisualizer()
        {
            this.Reset();
        }

        #region Customization properties
        public ProgressControllerViewModel Root
        {
            get { return this.viewModel; }
        }

        public bool ThrowIfAccessedNotFromUIThread
        {
            get;
            set;
        }
        #endregion

        #region Verification methods
        public void Reset()
        {
            this.viewModel = new ProgressControllerViewModel();
            this.isShown = false;
        }

        public void AssertIsShown()
        {
            Assert.IsTrue(this.isShown, "Expected to be shown");
        }

        public void AssertIsHidden()
        {
            Assert.IsFalse(this.isShown, "Expected to be hidden");
        }
        #endregion
    }
}
