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
    /// Progress controller helpers
    /// </summary>
    public static class Helpers
    {
        /// <summary>
        /// The <paramref name="onFinishedAction"/> will be called once when the <paramref name="controller"/>
        /// will finish by invoking <see cref="IProgressEvents.Finished"/>.
        /// </summary>
        /// <param name="controller">Required. <seealso cref="IProgressController"/></param>
        /// <param name="onFinishedAction">Required. The action that will be invoked with the finished <see cref="ProgressControllerResult"/></param>
        /// <remarks>This code will not cause memory leaks due to event registration</remarks>
        public static void RunOnFinished(this IProgressEvents controller, Action<ProgressControllerResult> onFinishedAction)
        {
            if (controller == null)
            {
                throw new ArgumentNullException(nameof(controller));
            }

            if (onFinishedAction == null)
            {
                throw new ArgumentNullException(nameof(onFinishedAction));
            }

            EventHandler<ProgressControllerFinishedEventArgs> onFinished = null;
            onFinished = (o, e) =>
            {
                controller.Finished -= onFinished;
                onFinishedAction.Invoke(e.Result);
            };

            controller.Finished += onFinished;
        }
    }
}
