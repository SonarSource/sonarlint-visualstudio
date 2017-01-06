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
using SonarLint.VisualStudio.Integration.Persistence;
using System;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    internal class ConfigurableSolutionBindingSerializer : ISolutionBindingSerializer
    {
        private int writtenFiles;

        #region ISolutionBindingSerializer
        BoundSonarQubeProject ISolutionBindingSerializer.ReadSolutionBinding()
        {
            this.ReadSolutionBindingAction?.Invoke();
            return this.CurrentBinding;
        }

        string ISolutionBindingSerializer.WriteSolutionBinding(BoundSonarQubeProject binding)
        {
            Assert.IsNotNull(binding, "Required argument");

            string filePath = this.WriteSolutionBindingAction?.Invoke(binding) ?? binding.ProjectKey;
            this.writtenFiles++;

            return filePath;
        }
        #endregion

        #region Test helpers
        public BoundSonarQubeProject CurrentBinding { get; set; }

        public void AssertWrittenFiles(int expected)
        {
            Assert.AreEqual(expected, this.writtenFiles, "Unexpected number of pending files");
        }

        public Func<BoundSonarQubeProject, string> WriteSolutionBindingAction { get; set; }

        public Action ReadSolutionBindingAction { get; set; }
        #endregion
    }
}
