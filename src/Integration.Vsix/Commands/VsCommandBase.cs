//-----------------------------------------------------------------------
// <copyright file="VsCommandBase.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using Microsoft.VisualStudio.Shell;
using System.Diagnostics;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    public abstract class VsCommandBase
    {
        private readonly IServiceProvider serviceProvider;

        protected IServiceProvider ServiceProvider => this.serviceProvider;

        protected VsCommandBase(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            this.serviceProvider = serviceProvider;
        }

        protected virtual void QueryStatusInternal(OleMenuCommand command)
        {
        }

        protected abstract void InvokeInternal();

        public void Invoke(object sender, EventArgs args)
        {
            var command = sender as OleMenuCommand;
            Debug.Assert(command != null, "Unexpected sender type; expected OleMenuCommand");
            Debug.Assert(command.Enabled, "Tried to invoke command without it being enabled");
            if (command.Enabled)
            {
                this.InvokeInternal();
            }
        }

        public void QueryStatus(object sender, EventArgs args)
        {
            var command = sender as OleMenuCommand;
            Debug.Assert(command != null, "Unexpected sender type; expected OleMenuCommand");
            this.QueryStatusInternal(command);
        }
    }
}
