/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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

using Microsoft.VisualStudio.PlatformUI;
using SonarLint.VisualStudio.Core;
using System.Windows.Navigation;

namespace SonarLint.VisualStudio.Integration.Vsix.Commands
{
    public sealed partial class AboutDialog : DialogWindow
    {
        public string SLVersion { get; private set; }

        private readonly IBrowserService browser;

        internal AboutDialog(IBrowserService browserService)
        {
            CacheExtensionVersion();
            this.browser = browserService;
            InitializeComponent();
            this.DataContext = this;
        }

        private void CacheExtensionVersion()
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location);
            SLVersion = fvi.FileVersion;
        }

        public void ViewWebsite(object sender, RequestNavigateEventArgs e)
        {
            browser.Navigate(e.Uri.AbsoluteUri);
        }
    }
}
