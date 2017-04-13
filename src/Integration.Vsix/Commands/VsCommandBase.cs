/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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
using System.Diagnostics;
using Microsoft.VisualStudio.Shell;

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
