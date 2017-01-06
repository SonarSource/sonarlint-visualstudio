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
    /// Factory representation to separate the <see cref="IProgressStepDefinition"/> from <see cref="IProgressStepOperation"/> and the execution notification using <see cref="IProgressStepExecutionEvents"/>
    /// </summary>
    public interface IProgressStepFactory
    {
        /// <summary>
        /// Create <see cref="CreateStepOperation"/> based on the <see cref="IProgressStepDefinition"/>
        /// </summary>
        /// <param name="controller">An instance of <see cref="IProgressController"/> for which to create the operation for <see cref="IProgressStepDefinition"/></param>
        /// <param name="definition">The definition to use when creating <see cref="IProgressStepOperation"/></param>
        /// <returns>An instance of <see cref="IProgressStepOperation"/></returns>
        IProgressStepOperation CreateStepOperation(IProgressController controller, IProgressStepDefinition definition);

        /// <summary>
        /// Returns a <see cref="IProgressStepExecutionEvents"/> that can be used to inform about the <see cref="IProgressStepOperation"/> execution changes
        /// </summary>
        /// <param name="stepOperation">The operation for which to fetch the <see cref="IProgressStepExecutionEvents"/></param>
        /// <returns>An instance of <see cref="IProgressStepExecutionEvents"/></returns>
        IProgressStepExecutionEvents GetExecutionCallback(IProgressStepOperation stepOperation);
    }
}
