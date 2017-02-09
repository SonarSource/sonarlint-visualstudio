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
using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.Progress.Observation;
using SonarLint.VisualStudio.Progress.Observation.ViewModels;

namespace SonarLint.VisualStudio.Progress.UnitTests
{
    /// <summary>
    /// Partial class implementation of <see cref="IProgressVisualizer"/>
    /// </summary>
    public partial class ConfigurableProgressVisualizer : IProgressVisualizer
    {
        ProgressControllerViewModel IProgressVisualizer.ViewModel
        {
            get
            {
                this.CheckUIThread();

                return this.viewModel;
            }

            set
            {
                this.CheckUIThread();

                this.viewModel = value;
            }
        }

        void IProgressVisualizer.Show()
        {
            this.CheckUIThread();

            this.isShown = true;
        }

        void IProgressVisualizer.Hide()
        {
            this.CheckUIThread();

            this.isShown = false;
        }

        private void CheckUIThread()
        {
            if (this.ThrowIfAccessedNotFromUIThread)
            {
                ThreadHelper.CheckAccess().Should().BeTrue("Wasn't called on the UI thread");
            }
        }
    }
}