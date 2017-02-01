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

using System.Windows.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Progress.MVVM;
using SonarLint.VisualStudio.Progress.Observation.ViewModels;

namespace SonarLint.VisualStudio.Progress.UnitTests
{
    [TestClass]
    public class ProgressControllerViewModelTests
    {
        [TestMethod]
        [Description("Verifies that all the publicly settable properties in ProgressControllerViewModel notify changes")]
        public void ProgressControllerViewModel_AllPublicPropertiesNotifyChanges()
        {
            ProgressControllerViewModel model = new ProgressControllerViewModel();
            ProgressStepViewModel step = new ProgressStepViewModel();
            model.Steps.Add(step);

            ViewModelVerifier.RunVerificationTest<ProgressControllerViewModel, string>(model, "Title", "value1", "value2");
            ViewModelVerifier.RunVerificationTest<ProgressControllerViewModel, ProgressStepViewModel>(model, "Current", null, step);
            ViewModelVerifier.RunVerificationTest<ProgressControllerViewModel, bool>(model, "Cancellable", true, false);
            ViewModelVerifier.RunVerificationTest<ProgressControllerViewModel, ICommand>(model, "CancelCommand", null, new RelayCommand((s) => { }));
        }
    }
}