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

using Microsoft.VisualStudio.CodeAnalysis.RuleSets;

namespace SonarLint.VisualStudio.Integration
{
    /// <summary>
    /// Abstraction for reading and writing of <see cref="RuleSet"/> instances.
    /// </summary>
    internal interface IRuleSetSerializer : ILocalService
    {
        /// <summary>
        /// Will write the specified <paramref name="ruleSet"/> into specified path.
        /// The caller needs to handler the various possible errors.
        /// </summary>
        void WriteRuleSetFile(RuleSet ruleSet, string path);

        /// <summary>
        /// Will load a RuleSet in specified <paramref name="path"/>.
        /// In case of error, null will be returned.
        /// </summary>
        RuleSet LoadRuleSet(string path);
    }
}
