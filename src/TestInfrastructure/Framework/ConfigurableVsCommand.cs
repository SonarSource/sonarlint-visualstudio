//-----------------------------------------------------------------------
// <copyright file="ConfigurableVsCommand.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.Integration.Vsix;
using System;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public class ConfigurableVsCommand : VsCommandBase
    {
        private readonly Action<OleMenuCommand> queryStatusFunc;

        public int InvokationCount { get; private set; } = 0;

        public ConfigurableVsCommand(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }

        public ConfigurableVsCommand(IServiceProvider serviceProvider, Action<OleMenuCommand> queryStatusFunc)
            : base(serviceProvider)
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

        #endregion
    }
}
