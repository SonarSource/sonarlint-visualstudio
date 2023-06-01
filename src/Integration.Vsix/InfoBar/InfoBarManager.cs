/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
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

            return AttachInfoBarToolWindowImpl(toolWindowGuid, message, imageMoniker, ButtonStyle.Button, buttonText);
        }

        public IInfoBar AttachInfoBarWithButtons(Guid toolWindowGuid, string message, IEnumerable<string> buttonTexts, SonarLintImageMoniker imageMoniker)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (buttonTexts == null)
            {
                throw new ArgumentNullException(nameof(buttonTexts));
            }

            return AttachInfoBarToolWindowImpl(toolWindowGuid, message, imageMoniker, ButtonStyle.Hyperlink, buttonTexts.ToArray());
        }

        public IInfoBar AttachInfoBar(Guid toolWindowGuid, string message, SonarLintImageMoniker imageMoniker)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentNullException(nameof(message));
            }

            return AttachInfoBarToolWindowImpl(toolWindowGuid, message, imageMoniker);
        }

        public IInfoBar AttachInfoBarToMainWindow(string message, SonarLintImageMoniker imageMoniker, params string[] buttonTexts)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (buttonTexts == null)
            {
                throw new ArgumentNullException(nameof(buttonTexts));
            }

            ThreadHelper.ThrowIfNotOnUIThread();

            // Note: this will return null if the VS main window hasn't been initialized
            // e.g. on startup when the startup dialog is visible
            if (!TryGetMainWindowInfoBarHost(out var host))
            {
                return null;
            }

            return AttachInfoBarImpl(host, message, imageMoniker, ButtonStyle.Hyperlink, buttonTexts);
        }

        public void DetachInfoBar(IInfoBar currentInfoBar)
        {
            if (currentInfoBar == null)
            {
                throw new ArgumentNullException(nameof(currentInfoBar));
            }

            if (!(currentInfoBar is PrivateInfoBarWrapper))
            {
                throw new ArgumentException(Strings.InvalidInfoBarInstance, nameof(currentInfoBar));
            }

            currentInfoBar.Close();
        }
        #endregion

        #region Helpers

        private IInfoBar AttachInfoBarToolWindowImpl(Guid toolWindowGuid, string message, SonarLintImageMoniker imageMoniker, ButtonStyle buttonStyle = ButtonStyle.Button, params string[] buttonTexts)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            IVsUIShell shell = serviceProvider.GetService<SVsUIShell, IVsUIShell>();
            IVsWindowFrame frame = GetToolWindowFrame(shell, toolWindowGuid);
            if (!TryGetInfoBarHost(frame, out var host))
            {
                return null;
            }

            var result = AttachInfoBarImpl(host, message, imageMoniker, buttonStyle, buttonTexts);
            if(result != null)
            {
                frame.ShowNoActivate();
            }

            return result;
        }

        private IInfoBar AttachInfoBarImpl(IVsInfoBarHost host, string message, SonarLintImageMoniker imageMoniker, ButtonStyle buttonStyle = ButtonStyle.Button, params string[] buttonTexts)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            InfoBarModel model = CreateModel(message, buttonTexts, buttonStyle, imageMoniker);
            IVsInfoBarUIFactory infoBarUIFactory = serviceProvider.GetService(typeof(SVsInfoBarUIFactory)) as IVsInfoBarUIFactory;
            IVsInfoBarUIElement uiElement;

            if (TryCreateInfoBarUI(infoBarUIFactory, model, out uiElement))
            {
                host.AddInfoBar(uiElement);
                return new PrivateInfoBarWrapper(host, uiElement);
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

        private static InfoBarModel CreateModel(string message, IEnumerable<string> buttonTexts, ButtonStyle buttonStyle, SonarLintImageMoniker imageMoniker)
        {
            var vsImageMoniker = new ImageMoniker { Guid = imageMoniker.Guid, Id = imageMoniker.Id };

            var actionItems = buttonTexts.Select(x =>
                buttonStyle == ButtonStyle.Button
                    ? (IVsInfoBarActionItem) new InfoBarButton(x)
                    : new InfoBarHyperlink(x));

            return new InfoBarModel(message,
                actionItems: actionItems,
                image: vsImageMoniker,
                isCloseButtonVisible: true);
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

        private bool TryGetMainWindowInfoBarHost(out IVsInfoBarHost infoBarHost)
        {
            var shell = serviceProvider.GetService(typeof(SVsShell)) as IVsShell;
            Debug.Assert(shell != null);

            shell.GetProperty((int)__VSSPROPID7.VSSPROPID_MainWindowInfoBarHost, out object value);

            infoBarHost = value as IVsInfoBarHost;
            return infoBarHost != null;
        }

        #endregion

        private enum ButtonStyle
        {
            Hyperlink,
            Button
        }

        private class PrivateInfoBarWrapper : IInfoBar, IVsInfoBarUIEvents
        {
            private uint? cookie;
            private bool isClosed;

            public PrivateInfoBarWrapper(IVsInfoBarHost host, IVsInfoBarUIElement uiElement)
            {
                Debug.Assert(host != null);
                Debug.Assert(uiElement != null);

                this.Host = host;
                this.InfoBarUIElement = uiElement;

                this.Advise();
            }

            private IVsInfoBarUIElement InfoBarUIElement { get; }

            private IVsInfoBarHost Host { get; }

            #region IInfoBarEvents
            public event EventHandler<InfoBarButtonClickedEventArgs> ButtonClick;
            public event EventHandler Closed;

            private void CloseUIElement()
            {
                if (this.isClosed)
                {
                    return;
                }

                ThreadHelper.ThrowIfNotOnUIThread();
                // This will fire the Closed event
                this.InfoBarUIElement.Close();

                Debug.Assert(!this.cookie.HasValue, "Expected to be unadvised");
            }

            public void Close()
            {
                CloseUIElement();

                ThreadHelper.ThrowIfNotOnUIThread();
                if (Host != null )
                {
                    Host.RemoveInfoBar(InfoBarUIElement);
                }
            }

            #endregion

            #region IVsInfoBarUIEvents
            void IVsInfoBarUIEvents.OnActionItemClicked(IVsInfoBarUIElement infoBarUIElement, IVsInfoBarActionItem actionItem)
            {
                this.ButtonClick?.Invoke(this, new InfoBarButtonClickedEventArgs(actionItem.Text));
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
