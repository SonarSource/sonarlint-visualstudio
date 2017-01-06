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
using System.Collections.ObjectModel;
using System.ComponentModel.Design;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    /// <summary>
    /// Test implementation of <see cref="IMenuCommandService"/>.
    /// </summary>
    public class ConfigurableMenuCommandService : IMenuCommandService
    {
        private readonly IDictionary<CommandID, MenuCommand> commands = new Dictionary<CommandID, MenuCommand>();

        public IReadOnlyDictionary<CommandID, MenuCommand> Commands
        {
            get
            {
                return new ReadOnlyDictionary<CommandID, MenuCommand>(this.commands);
            }
        }

        #region IMenuCommandService

        DesignerVerbCollection IMenuCommandService.Verbs
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        void IMenuCommandService.AddCommand(MenuCommand command)
        {
            this.commands.Add(command.CommandID, command);
        }

        void IMenuCommandService.AddVerb(DesignerVerb verb)
        {
            throw new NotImplementedException();
        }

        MenuCommand IMenuCommandService.FindCommand(CommandID commandID)
        {
            return this.commands[commandID];
        }

        bool IMenuCommandService.GlobalInvoke(CommandID commandID)
        {
            throw new NotImplementedException();
        }

        void IMenuCommandService.RemoveCommand(MenuCommand command)
        {
            throw new NotImplementedException();
        }

        void IMenuCommandService.RemoveVerb(DesignerVerb verb)
        {
            throw new NotImplementedException();
        }

        void IMenuCommandService.ShowContextMenu(CommandID menuID, int x, int y)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
