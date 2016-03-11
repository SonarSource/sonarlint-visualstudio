//-----------------------------------------------------------------------
// <copyright file="ConfigurableBindingOperation.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarLint.VisualStudio.Integration.Binding;
using System;
using System.Threading;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    internal class ConfigurableBindingOperation : IBindingOperation
    {
        #region IBindingOperation
        void IBindingOperation.Initialize()
        {
            this.InitializeAction?.Invoke();
        }

        void IBindingOperation.Commit()
        {
            this.CommitAction?.Invoke();
        }

        void IBindingOperation.Prepare(CancellationToken token)
        {
            this.PrepareAction?.Invoke(token);
        }
        #endregion


        #region Test helpers
        public Action InitializeAction { get; set; }

        public Action<CancellationToken> PrepareAction { get; set; }

        public Action CommitAction { get; set; }
        #endregion
    }
}
