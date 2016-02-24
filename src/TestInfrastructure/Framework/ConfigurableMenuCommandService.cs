//-----------------------------------------------------------------------
// <copyright file="ConfigurableMenuCommandService.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

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
