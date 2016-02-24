//-----------------------------------------------------------------------
// <copyright file="ConnectSectionView.xaml.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarLint.VisualStudio.Integration.Progress;
using System.Windows.Controls;

namespace SonarLint.VisualStudio.Integration.TeamExplorer
{
    /// <summary>
    /// Interaction logic for ConnectSectionView.xaml
    /// </summary>
    public partial class ConnectSectionView : UserControl, IProgressControlHost
    {
        public ConnectSectionView()
        {
            InitializeComponent();
        }

        void IProgressControlHost.Host(ProgressControl progressControl)
        {
            this.HostProgressControl(progressControl);
        }

        protected virtual void HostProgressControl(ProgressControl control)
        {
            this.progressPlacePlaceholder.Content = control;
        }
    }
}
