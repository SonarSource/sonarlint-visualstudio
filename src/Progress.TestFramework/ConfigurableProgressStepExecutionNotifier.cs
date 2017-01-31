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

using System;
using System.Collections.Generic;
using SonarLint.VisualStudio.Progress.Controller;

namespace SonarLint.VisualStudio.Progress.UnitTests
{
    /// <summary>
    /// Test helper class to monitor the progress change notification (implements <see cref="IProgressStepExecutionEvents"/>)
    /// </summary>
    public class ConfigurableProgressStepExecutionNotifier : IProgressStepExecutionEvents
    {
        public ConfigurableProgressStepExecutionNotifier()
        {
            this.ProgressChanges = new List<Tuple<string, double>>();
        }

        #region Verification
        public List<Tuple<string, double>> ProgressChanges
        {
            get;
            set;
        }
        #endregion

        #region Configuration
        public Action<string, double> ProgressChangedAction
        {
            get;
            set;
        }
        #endregion

        #region Test implementation of IProgressStepExecutionEvents  (not to be used explicitly by the test code)
        void IProgressStepExecutionEvents.ProgressChanged(string progressDetailText, double progress)
        {
            this.ProgressChanges.Add(Tuple.Create(progressDetailText, progress));

            if (this.ProgressChangedAction != null)
            {
                this.ProgressChangedAction(progressDetailText, progress);
            }
        }
        #endregion
    }
}
