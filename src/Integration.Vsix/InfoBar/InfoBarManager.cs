//-----------------------------------------------------------------------
// <copyright file="InfoBarManager.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Integration.InfoBar;
using SonarLint.VisualStudio.Integration.Vsix.Resources;
using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Globalization;

namespace SonarLint.VisualStudio.Integration.Vsix.InfoBar
{
    [Export(typeof(IInfoBarManager))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class InfoBarManager : IInfoBarManager
    {
        private readonly IServiceProvider serviceProvider;

        [ImportingConstructor]
        public InfoBarManager([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            this.serviceProvider = serviceProvider;
        }

        #region IInfoBarManager
        public IInfoBar AttachInfoBar(Guid toolwindowGuid, string message, string buttonText, ImageMoniker imageMoniker)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (string.IsNullOrWhiteSpace(buttonText))
            {
                throw new ArgumentNullException(nameof(buttonText));
            }

            IVsWindowFrame frame = GetToolWindowFrame(this.serviceProvider, toolwindowGuid);

            InfoBarModel model = CreateModel(message, buttonText, imageMoniker);

            IVsInfoBarUIElement uiElement;
            if (TryCreateInfoBarUI(this.serviceProvider, model, out uiElement)
                && TryAddInfoBarToFrame(frame, uiElement))
            {
                return new PrivateInfoBarWrapper(frame, uiElement);
            }

            return null;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability",
            "S3215:\"interface\" instances should not be cast to concrete types",
            Justification = "We want to hide the concrete implementation but also handle instance that were created by this class in the first place",
            Scope = "member",
            Target = "~M:SonarLint.VisualStudio.Integration.Vsix.InfoBar.InfoBarManager.DetachInfoBar(SonarLint.VisualStudio.Integration.InfoBar.IInfoBar)")]
        public void DetachInfoBar(IInfoBar currentInfoBar)
        {
            if (currentInfoBar == null)
            {
                throw new ArgumentNullException(nameof(currentInfoBar));
            }

            PrivateInfoBarWrapper wrapper = currentInfoBar as PrivateInfoBarWrapper;
            if (wrapper == null)
            {
                throw new ArgumentException(Strings.InvalidInfoBarInstance, nameof(currentInfoBar));
            }

            currentInfoBar.Close();

            IVsInfoBarHost host;
            if (TryGetInfoBarHost(wrapper.Frame, out host))
            {
                host.RemoveInfoBar(wrapper.InfoBarUIElement);
            }
        }
        #endregion

        #region Static helpers
        private static IVsWindowFrame GetToolWindowFrame(IServiceProvider serviceProvider, Guid toolwindowGuid)
        {
            Debug.Assert(serviceProvider != null);

            IVsUIShell shell = serviceProvider.GetService<SVsUIShell, IVsUIShell>();
            IVsWindowFrame frame;
            int hr = shell.FindToolWindow((uint)__VSFINDTOOLWIN.FTW_fForceCreate, ref toolwindowGuid, out frame);
            if (ErrorHandler.Failed(hr) || frame == null)
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Strings.CannotFindToolWindow, toolwindowGuid), nameof(toolwindowGuid));
            }

            return frame;
        }

        private static bool TryCreateInfoBarUI(IServiceProvider serviceProvider, IVsInfoBar infoBar, out IVsInfoBarUIElement uiElement)
        {
            Debug.Assert(serviceProvider != null);
            Debug.Assert(infoBar != null);

            IVsInfoBarUIFactory infoBarUIFactory = serviceProvider.GetService(typeof(SVsInfoBarUIFactory)) as IVsInfoBarUIFactory;

            if (infoBarUIFactory == null)
            {
                uiElement = null;
                return false;
            }

            uiElement = infoBarUIFactory.CreateInfoBar(infoBar);

            return uiElement != null;
        }

        private static InfoBarModel CreateModel(string message, string buttonText, ImageMoniker imageMoniker)
        {
            return new InfoBarModel(message,
                actionItems: new[]
                {
                    new InfoBarButton(buttonText)
                },
                image: imageMoniker,
                isCloseButtonVisible: true);
        }

        private static bool TryAddInfoBarToFrame(IVsWindowFrame frame, IVsUIElement uiElement)
        {
            IVsInfoBarHost infoBarHost;
            if (TryGetInfoBarHost(frame, out infoBarHost))
            {
                infoBarHost.AddInfoBar(uiElement);
                return true;
            }

            return false;
        }

        private static bool TryGetInfoBarHost(IVsWindowFrame frame, out IVsInfoBarHost infoBarHost)
        {
            object infoBarHostObj;

            if (ErrorHandler.Failed(frame.GetProperty((int)__VSFPROPID7.VSFPROPID_InfoBarHost, out infoBarHostObj)))
            {
                infoBarHost = null;
                return false;
            }

            infoBarHost = infoBarHostObj as IVsInfoBarHost;
            return infoBarHost != null;
        }
        #endregion

        private class PrivateInfoBarWrapper : IInfoBar, IVsInfoBarUIEvents
        {
            private uint? cookie;
            private bool isClosed;

            public PrivateInfoBarWrapper(IVsWindowFrame frame, IVsInfoBarUIElement uiElement)
            {
                Debug.Assert(frame != null);
                Debug.Assert(uiElement != null);

                this.InfoBarUIElement = uiElement;
                this.Frame = frame;

                this.Frame.ShowNoActivate();

                this.Advise();
            }

            internal IVsInfoBarUIElement InfoBarUIElement { get; }

            internal IVsWindowFrame Frame { get; }

            #region IInfoBarEvents
            public event EventHandler ButtonClick;
            public event EventHandler Closed;

            public void Close()
            {
                if (this.isClosed)
                {
                    return;
                }

                this.InfoBarUIElement.Close();

                Debug.Assert(!this.cookie.HasValue, "Expected to be unadvised");
            }
            #endregion

            #region IVsInfoBarUIEvents
            void IVsInfoBarUIEvents.OnActionItemClicked(IVsInfoBarUIElement uiElement, IVsInfoBarActionItem actionItem)
            {
                this.ButtonClick?.Invoke(this, EventArgs.Empty);
            }

            void IVsInfoBarUIEvents.OnClosed(IVsInfoBarUIElement uiElement)
            {
                this.Unadvise();
                this.isClosed = true;

                this.Closed?.Invoke(this, EventArgs.Empty);
            }
            #endregion

            #region Helpers
            private void Advise()
            {
                uint syncCookie;

                Debug.Assert(!this.cookie.HasValue, "Already advised");

                if (ErrorHandler.Succeeded(this.InfoBarUIElement.Advise(this, out syncCookie)))
                {
                    this.cookie = syncCookie;
                }
                else
                {
                    Debug.Fail("Failed in IVsInfoBarUIElement.Advise");
                }
            }

            private void Unadvise()
            {
                Debug.Assert(this.cookie.HasValue, "Already unadvised");

                if (ErrorHandler.Succeeded(this.InfoBarUIElement.Unadvise(this.cookie.Value)))
                {
                    this.cookie = null;
                }
                else
                {
                    Debug.Fail("Failed in IVsInfoBarUIElement.Unadvise");
                }
            }
            #endregion
        }
    }
}
