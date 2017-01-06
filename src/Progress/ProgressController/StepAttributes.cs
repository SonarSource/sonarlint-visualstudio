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

namespace SonarLint.VisualStudio.Progress.Controller
{
    /// <summary>
    /// Step attribute flags
    /// <seealso cref="ProgressStepDefinition"/>
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue", Justification = "None represents zero in bits")]
    [Flags]
    public enum StepAttributes
    {
        /// <summary>
        /// Cancellable, Visible, Determinate, Foreground thread and impacting on the overall progress
        /// </summary>
        None = 0,

        /// <summary>
        /// Background thread flag
        /// </summary>
        BackgroundThread = 1,

        /// <summary>
        /// The step is not cancellable
        /// </summary>
        /// <seealso cref="IProgressStep.Cancellable"/>
        NonCancellable = 2,

        /// <summary>
        /// Hidden flag
        /// <seealso cref="IProgressStep.Hidden"/>
        /// </summary>
        Hidden = 4,

        /// <summary>
        /// Indeterminate progress flag
        /// </summary>
        /// <seealso cref="IProgressStep.Indeterminate"/>
        Indeterminate = 8,

        /// <summary>
        /// Does not impact the overall progress
        /// </summary>
        /// <seealso cref="IProgressStep.ImpactsProgress"/>
        NoProgressImpact = 16
    }
}
