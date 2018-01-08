/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
 * mailto:info AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
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

        #endregion IMenuCommandService
    }
}