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

namespace SonarLint.VisualStudio.Integration.Persistence
{
    // Test interface
    internal interface ISolutionBindingSerializer : ILocalService
    {
        /// <summary>
        /// Retrieves solution binding information
        /// </summary>
        /// <returns>Can be null if not bound</returns>
        BoundSonarQubeProject ReadSolutionBinding();

        /// <summary>
        /// Writes the binding information
        /// </summary>
        /// <param name="binding">Required</param>
        /// <param name="sccFileSystem">Required</param>
        /// <returns>The file path to the binding file</returns>
        string WriteSolutionBinding(BoundSonarQubeProject binding);
    }
}
