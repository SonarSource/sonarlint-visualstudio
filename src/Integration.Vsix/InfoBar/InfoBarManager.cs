/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Globalization;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.InfoBar;
using SonarLint.VisualStudio.Integration.Vsix.Resources;
using ErrorHandler = Microsoft.VisualStudio.ErrorHandler;

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
            this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        #region IInfoBarManager
        public IInfoBar AttachInfoBarWithButton(Guid toolWindowGuid, string message, string buttonText, SonarLintImageMoniker imageMoniker)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (string.IsNullOrWhiteSpace(buttonText))
            {
                throw new ArgumentNullException(nameof(buttonText));
            }

            return AttachInfoBarImpl(toolWindowGuid, message, buttonText, imageMoniker);
        }

        public IInfoBar AttachInfoBar(Guid toolWindowGuid, string message, SonarLintImageMoniker imageMoniker)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentNullException(nameof(message));
            }

            return AttachInfoBarImpl(toolWindowGuid, message, null, imageMoniker);
        }

        public void DetachInfoBar(IInfoBar currentInfoBar)
        {
            if (currentInfoBar == null)
            {
                throw new ArgumentNullException(nameof(currentInfoBar));
            }

            if (!(currentInfoBar is PrivateInfoBarWrapper wrapper))
            {
                throw new ArgumentException(Strings.InvalidInfoBarInstance, nameof(currentInfoBar));
            }

            currentInfoBar.Close();

            ThreadHelper.ThrowIfNotOnUIThread();
            IVsInfoBarHost host;
            if (TryGetInfoBarHost(wrapper.Frame, out host))
            {
                host.RemoveInfoBar(wrapper.InfoBarUIElement);
            }
        }
        #endregion

        #region Static helpers

        private IInfoBar AttachInfoBarImpl(Guid toolWindowGuid, string message, string buttonText, SonarLintImageMoniker imageMoniker)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            IVsUIShell shell = serviceProvider.GetService<SVsUIShell, IVsUIShell>();
            IVsWindowFrame frame = GetToolWindowFrame(shell, toolWindowGuid);

            InfoBarModel model = CreateModel(message, buttonText, imageMoniker);

            IVsInfoBarUIFactory infoBarUIFactory = serviceProvider.GetService<SVsInfoBarUIFactory, IVsInfoBarUIFactory>();
            IVsInfoBarUIElement uiElement;
            if (TryCreateInfoBarUI(infoBarUIFactory, model, out uiElement)
                && TryAddInfoBarToFrame(frame, uiElement))
            {
                return new PrivateInfoBarWrapper(frame, uiElement);
            }

            return null;
        }

        private static IVsWindowFrame GetToolWindowFrame(IVsUIShell shell, Guid toolwindowGuid)
        {
            Debug.Assert(shell != null);

            IVsWindowFrame frame;
            int hr = shell.FindToolWindow((uint)__VSFINDTOOLWIN.FTW_fForceCreate, ref toolwindowGuid, out frame);
            if (ErrorHandler.Failed(hr) || frame == null)
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Strings.CannotFindToolWindow, toolwindowGuid), nameof(toolwindowGuid));
            }

            return frame;
        }

        private static bool TryCreateInfoBarUI(IVsInfoBarUIFactory infoBarUIFactory, IVsInfoBar infoBar, out IVsInfoBarUIElement uiElement)
        {
            Debug.Assert(infoBar != null);

            if (infoBarUIFactory == null)
            {
                uiElement = null;
                return false;
            }

            uiElement = infoBarUIFactory.CreateInfoBar(infoBar);

            return uiElement != null;
        }

        private static InfoBarModel CreateModel(string message, string buttonText, SonarLintImageMoniker imageMoniker)
        {
            var vsImageMoniker = new ImageMoniker {Guid = imageMoniker.Guid, Id = imageMoniker.Id};

            return new InfoBarModel(message,
                actionItems: buttonText != null ? new[] { new InfoBarButton(buttonText) } : new IVsInfoBarActionItem[0],
                image: vsImageMoniker,
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

                ThreadHelper.ThrowIfNotOnUIThread();
                this.InfoBarUIElement.Close();

                Debug.Assert(!this.cookie.HasValue, "Expected to be unadvised");
            }
            #endregion

            #region IVsInfoBarUIEvents
            void IVsInfoBarUIEvents.OnActionItemClicked(IVsInfoBarUIElement infoBarUIElement, IVsInfoBarActionItem actionItem)
            {
                this.ButtonClick?.Invoke(this, EventArgs.Empty);
            }

            void IVsInfoBarUIEvents.OnClosed(IVsInfoBarUIElement infoBarUIElement)
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
