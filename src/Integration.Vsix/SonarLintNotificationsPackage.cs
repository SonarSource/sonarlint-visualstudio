//------------------------------------------------------------------------------
// <copyright file="SonarLintNotifications.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Integration.Vsix.Notifications;
using SystemWrapper.Timers;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [Guid(PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    [ProvideAutoLoad(UIContextGuids.NoSolution)]
    [ProvideAutoLoad(UIContextGuids.SolutionExists)]
    public sealed class SonarLintNotificationsPackage : Package
    {
        /// <summary>
        /// SonarLintNotifications GUID string.
        /// </summary>
        public const string PackageGuidString = "c26b6802-dd9c-4a49-b8a5-0ad8ef04c579";

        private SonarQubeNotifications notifications;
        private IActiveSolutionBoundTracker activeSolutionBoundTracker;

        public SonarLintNotificationsPackage()
        {
        }

        protected override void Initialize()
        {
            base.Initialize();

            notifications = new SonarQubeNotifications(new NotifyIconFactory(), new TimerFactory());

            activeSolutionBoundTracker = this.GetMefService<IActiveSolutionBoundTracker>();
            activeSolutionBoundTracker.SolutionBindingChanged += OnSolutionBindingChanged;
        }

        private void OnSolutionBindingChanged(object sender, bool isBound)
        {
            if (isBound)
            {
                notifications.Start();
            }
            else
            {
                notifications.Stop();
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            activeSolutionBoundTracker.SolutionBindingChanged -= OnSolutionBindingChanged;

            if (notifications != null)
            {
                notifications.Dispose();
                notifications = null;
            }
        }
    }
}
