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
using SonarLint.VisualStudio.Progress.Controller;
using SonarLint.VisualStudio.Progress.Observation.ViewModels;

namespace SonarLint.VisualStudio.Progress.UnitTests
{
    [TestClass]
    public class ProgressStepViewModelTests
    {
        [TestMethod]
        [Description("Verifies that all the publicly settable properties in ProgressStepViewModel notify changes")]
        public void ProgressStepViewModel_AllPublicPropertiesNotifyChanges()
        {
            ProgressStepViewModel model = new ProgressStepViewModel();

            ViewModelVerifier.RunVerificationTest<ProgressStepViewModel, string>(model, "DisplayText", "value1", "value2");
            ViewModelVerifier.RunVerificationTest<ProgressStepViewModel, StepExecutionState>(model, "ExecutionState", StepExecutionState.Cancelled, StepExecutionState.Failed);
            ViewModelVerifier.RunVerificationTest<ProgressStepViewModel, string>(model, "ProgressDetailText", null, string.Empty);
        }
    }
}
