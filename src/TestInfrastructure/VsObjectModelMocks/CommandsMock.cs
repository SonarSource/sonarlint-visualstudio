//-----------------------------------------------------------------------
// <copyright file="CommandsMock.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using EnvDTE;
using System;
using System.Collections;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public class CommandsMock : Commands
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
        #endregion

        #region Test helpers
     
        public Action<Guid,int> RaiseAction
        {
            get;
            set;
        }
        #endregion
    }
}
