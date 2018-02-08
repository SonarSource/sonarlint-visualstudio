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
using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.Integration.Vsix;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    internal class ConfigurableVsCommand : VsCommandBase
    {
        private readonly Action<OleMenuCommand> queryStatusFunc;

        public int InvokationCount { get; private set; } = 0;

        public ConfigurableVsCommand()
        {
        }

        public ConfigurableVsCommand(Action<OleMenuCommand> queryStatusFunc)
        {
            this.queryStatusFunc = queryStatusFunc;
        }

        #region VsCommandBase

        protected override void InvokeInternal()
        {
            this.InvokationCount++;
        }

        protected override void QueryStatusInternal(OleMenuCommand command)
        {
            this.queryStatusFunc?.Invoke(command);
        }

        #endregion VsCommandBase
    }
}