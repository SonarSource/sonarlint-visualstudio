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
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Options;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    public class WorkspaceConfigurator : IWorkspaceConfigurator
    {
        private readonly Workspace workspace;

        public WorkspaceConfigurator(Workspace workspace)
        {
            if (workspace == null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }

            this.workspace = workspace;
        }

        public void ToggleBooleanOptionKey(OptionKey optionKey)
        {
            bool current = (bool)workspace.Options.GetOption(optionKey);
            OptionSet newOptions = workspace.Options.WithChangedOption(optionKey, !current);
            workspace.Options = newOptions;
        }

        [ExcludeFromCodeCoverage] // Uses reflection
        public IOption FindOptionByName(string feature, string name)
        {
            object localOptionService = FindOptionService(workspace);
            Debug.Assert(localOptionService != null);

            const string methodName = "GetRegisteredOptions";
            MethodInfo getOptionsMethod = localOptionService.GetType()?.GetMethod(methodName);
            Debug.Assert(getOptionsMethod != null);

            IEnumerable<IOption> options = getOptionsMethod?.Invoke(localOptionService, null) as IEnumerable<IOption>;

            options = options?.Where(o => o.Feature.Equals(feature, StringComparison.Ordinal) && o.Name.Equals(name, StringComparison.Ordinal));
            return options?.FirstOrDefault();
        }

        [ExcludeFromCodeCoverage] // Uses reflection
        private static object FindOptionService(Workspace workspace)
        {
            const string optionServiceTypeName = "Microsoft.CodeAnalysis.Options.IOptionService";
            const string getServiceMethod = "GetService";

            Type optionServiceType = typeof(IOption).Assembly.GetType(optionServiceTypeName, false);
            Debug.Assert(optionServiceType != null);

            return workspace.Services.GetType()
                    ?.GetMethod(getServiceMethod)
                    ?.MakeGenericMethod(optionServiceType)
                    ?.Invoke(workspace.Services, null);
        }
    }
}
