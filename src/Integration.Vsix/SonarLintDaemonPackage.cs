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

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Windows;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [Guid(PackageGuidString)]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    public sealed class SonarLintDaemonPackage : Package
    {
        public const string PackageGuidString = "6f63ab5a-5ab8-4a0d-9914-151911885966";

        private readonly ISonarLintSettings settings = ServiceProvider.GlobalProvider.GetMefService<ISonarLintSettings>();
        private readonly ISonarLintDaemon daemon = ServiceProvider.GlobalProvider.GetMefService<ISonarLintDaemon>();

        /// <summary>
        /// Initializes a new instance of the <see cref="SonarLintDaemonPackage"/> class.
        /// </summary>
        public SonarLintDaemonPackage()
        {
            // Inside this method you can place any initialization code that does not require
            // any Visual Studio service because at this point the package object is created but
            // not sited yet inside Visual Studio environment. The place to do all the other
            // initialization is the Initialize method.
        }

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            if (settings.IsActivateMoreEnabled && daemon.IsInstalled)
            {
                if (!daemon.IsRunning)
                {
                    daemon.Start();
                }
            }
            else if (!SkipActivateMoreDialog())
            {
                LaunchActivateMoreDialog();
            }
        }

        private bool SkipActivateMoreDialog()
        {
            return ServiceProvider.GlobalProvider.GetMefService<ISonarLintSettings>().SkipActivateMoreDialog;
        }

        private void LaunchActivateMoreDialog()
        {
            var title = "Activate SonarJS support in SonarLint";
            var message = "SonarLint for Visual Studio can also analyze JavaScript files, limited to standalone mode for now. After installing JavaScript support, it will be activated for newly opened files.\n\nWould you like to download and activate SonarJS support in SonarLint now?";
            var result = MessageBox.Show(message, title, MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                settings.IsActivateMoreEnabled = true;
                new SonarLintDaemonInstaller().Show();
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                daemon.Dispose();
            }
        }

        #endregion
    }
}
