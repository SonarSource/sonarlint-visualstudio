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
using System.Collections;
using EnvDTE;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    internal class CommandsMock : Commands
    {
        private readonly DTEMock dte;

        public CommandsMock(DTEMock dte = null)
        {
            this.dte = dte;
        }

        #region Commands

        int Commands.Count
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        DTE Commands.DTE
        {
            get
            {
                return this.dte;
            }
        }

        DTE Commands.Parent
        {
            get
            {
                return this.dte;
            }
        }

        void Commands.Add(string Guid, int ID, ref object Control)
        {
            throw new NotImplementedException();
        }

        object Commands.AddCommandBar(string Name, vsCommandBarType Type, object CommandBarParent, int Position)
        {
            throw new NotImplementedException();
        }

        Command Commands.AddNamedCommand(AddIn AddInInstance, string Name, string ButtonText, string Tooltip, bool MSOButton, int Bitmap, ref object[] ContextUIGUIDs, int vsCommandDisabledFlagsValue)
        {
            throw new NotImplementedException();
        }

        void Commands.CommandInfo(object CommandBarControl, out string Guid, out int ID)
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator Commands.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        Command Commands.Item(object index, int ID)
        {
            throw new NotImplementedException();
        }

        void Commands.Raise(string Guid, int ID, ref object CustomIn, ref object CustomOut)
        {
            CustomIn = null;
            CustomOut = null;

            var commandGroup = new System.Guid(Guid);
            this.RaiseAction?.Invoke(commandGroup, ID);
        }

        void Commands.RemoveCommandBar(object CommandBar)
        {
            throw new NotImplementedException();
        }

        #endregion Commands

        #region Test helpers

        public Action<Guid, int> RaiseAction
        {
            get;
            set;
        }

        #endregion Test helpers
    }
}