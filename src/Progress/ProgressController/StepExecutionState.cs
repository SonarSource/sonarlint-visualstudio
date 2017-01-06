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

namespace SonarLint.VisualStudio.Progress.Controller
{
    /// <summary>
    /// The <see cref="IProgressStep"/> execution state
    /// </summary>
    public enum StepExecutionState
    {
        /// <summary>
        /// Execution has not been started
        /// </summary>
        NotStarted,

        /// <summary>
        /// Executing an <see cref="IProgressStepOperation"/>
        /// </summary>
        Executing,

        /// <summary>
        /// An error was thrown while executing <see cref="IProgressStepOperation"/>
        /// </summary>
        Failed,

        /// <summary>
        /// The execution was canceled while executing <see cref="IProgressStepOperation"/>
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId = "Cancelled", Justification = "The preferred term has a typo")]
        Cancelled,

        /// <summary>
        /// No error had occurred while executing <see cref="IProgressStepOperation"/>
        /// </summary>
        Succeeded
    }
}
