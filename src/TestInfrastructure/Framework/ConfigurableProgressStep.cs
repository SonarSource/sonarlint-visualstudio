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
using SonarLint.VisualStudio.Progress.Controller;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public class ConfigurableProgressStep : IProgressStep
    {
        public bool Cancellable
        {
            get;
            set;
        }

        public string DisplayText
        {
            get;
            set;
        }

        public StepExecutionState ExecutionState
        {
            get;
            set;
        }

        public bool Hidden
        {
            get;
            set;
        }

        public bool ImpactsProgress
        {
            get;
            set;
        }

        public bool Indeterminate
        {
            get;
            set;
        }

        public double Progress
        {
            get;
            set;
        }

        public string ProgressDetailText
        {
            get;
            set;
        }

#pragma warning disable 67

        public event EventHandler<StepExecutionChangedEventArgs> StateChanged;

#pragma warning restore 67
    }
}