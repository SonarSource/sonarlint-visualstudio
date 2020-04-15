/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
using System.ComponentModel.Design;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    internal class DummyMenuCommandService : IMenuCommandService
    {
        public IList<MenuCommand> AddedMenuCommands { get; } = new List<MenuCommand>();

        #region IMenuCommandService methods 

        public DesignerVerbCollection Verbs => throw new NotImplementedException();

        public void AddCommand(MenuCommand command) => AddedMenuCommands.Add(command);

        public void AddVerb(DesignerVerb verb) => throw new NotImplementedException();
        public MenuCommand FindCommand(CommandID commandID) => throw new NotImplementedException();
        public bool GlobalInvoke(CommandID commandID) => throw new NotImplementedException();
        public void RemoveCommand(MenuCommand command) => throw new NotImplementedException();
        public void RemoveVerb(DesignerVerb verb) => throw new NotImplementedException();
        public void ShowContextMenu(CommandID menuID, int x, int y) => throw new NotImplementedException();

        #endregion IMenuCommandService methods 
    }
}
