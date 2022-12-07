/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2022 SonarSource SA
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

using System.Windows.Controls;
using Microsoft.VisualStudio.PlatformUI;
using System.Windows.Forms;
using System.Reflection;
using System.Diagnostics;
using SonarLint.VisualStudio.Core;
using System.Windows.Navigation;
using System;
using System.Windows.Media.Imaging;

namespace SonarLint.VisualStudio.Integration.Vsix.Commands
{
    public sealed partial class AboutDialog : DialogWindow
    {
        public string SLVersion { get; private set; }

        private readonly IBrowserService browser;

        internal AboutDialog(IBrowserService browserService)
        {
            SetVersion();
            this.browser = browserService;
            InitializeComponent();
            this.DataContext = this;
        }

        private void SetVersion()
        {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            System.Diagnostics.FileVersionInfo fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location);
            SLVersion = "Version: " + fvi.FileVersion;
        }

        public void ViewWebsite(object sender, RequestNavigateEventArgs e)
        {
            browser.Navigate(e.Uri.AbsoluteUri);
        }
    }
}
