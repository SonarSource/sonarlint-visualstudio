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
using System;
using System.Diagnostics;
using System.IO;
using System.Xml;

namespace SonarLint.VisualStudio.Integration
{
    internal sealed class RuleSetSerializer : IRuleSetSerializer
    {
        private readonly IServiceProvider serviceProvider;

        public RuleSetSerializer(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            this.serviceProvider = serviceProvider;
        }

        public RuleSet LoadRuleSet(string ruleSetPath)
        {
            if (string.IsNullOrWhiteSpace(ruleSetPath))
            {
                throw new ArgumentNullException(nameof(ruleSetPath));
            }

            var fileSystem = this.serviceProvider.GetService<IFileSystem>();
            fileSystem.AssertLocalServiceIsNotNull();

            if (fileSystem.FileExist(ruleSetPath))
            {
                try
                {
                    return RuleSet.LoadFromFile(ruleSetPath);
                }
                catch (Exception ex) when (ex is InvalidRuleSetException || ex is XmlException || ex is IOException)
                {
                    // Log this for testing purposes
                    Trace.WriteLine(ex.ToString(), nameof(LoadRuleSet));
                }
            }

            return null;

        }

        public void WriteRuleSetFile(RuleSet ruleSet, string ruleSetPath)
        {
            if (ruleSet == null)
            {
                throw new ArgumentNullException(nameof(ruleSet));
            }

            if (string.IsNullOrWhiteSpace(ruleSetPath))
            {
                throw new ArgumentNullException(nameof(ruleSetPath));
            }

            ruleSet.WriteToFile(ruleSetPath);
        }
    }
}
