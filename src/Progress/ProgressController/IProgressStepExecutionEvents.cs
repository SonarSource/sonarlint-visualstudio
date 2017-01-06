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
    /// The interface is used to notify <see cref="IProgressController"/> of <see cref="IProgressStep"/> changes during execution
    /// <seealso cref="IProgressStepOperation"/>
    /// </summary>
    public interface IProgressStepExecutionEvents
    {
        /// <summary>
        /// Progress change notification
        /// </summary>
        /// <param name="progressDetailText">Optional (can be null)</param>
        /// <param name="progress">The execution progress</param>
        void ProgressChanged(string progressDetailText, double progress = double.NaN);
    }
}
