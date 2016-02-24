//-----------------------------------------------------------------------
// <copyright file="TestableConnectSectionView.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarLint.VisualStudio.Integration.Progress;
using System.Windows;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    internal class TestableConnectSectionView : DependencyObject, IProgressControlHost
    {
        #region IProgressControlHost
        void IProgressControlHost.Host(ProgressControl progressControl)
        {
        }
        #endregion
    }
}
