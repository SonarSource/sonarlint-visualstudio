//-----------------------------------------------------------------------
// <copyright file="ConfigurableInfoBarManager.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.InfoBar;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    internal class ConfigurableInfoBarManager : IInfoBarManager
    {
        private readonly Dictionary<Guid, ConfigurableInfoBar> attached = new Dictionary<Guid, ConfigurableInfoBar>();

        #region IInfoBarManager
        IInfoBar IInfoBarManager.AttachInfoBar(Guid toolwindowGuid, string message, string buttonText, ImageMoniker imageMoniker)
        {
            Assert.IsFalse(this.attached.ContainsKey(toolwindowGuid), "Info bar is already attached to tool window {0}", toolwindowGuid);

            var infoBar = new ConfigurableInfoBar(message, buttonText, imageMoniker);
            this.attached[toolwindowGuid] = infoBar;
            return infoBar;
        }

        void IInfoBarManager.DetachInfoBar(IInfoBar currentInfoBar)
        {
            Assert.IsTrue(this.attached.Values.Contains(currentInfoBar), "Info bar is not attached");
            this.attached.Remove(attached.Single(kv => kv.Value == currentInfoBar).Key);
        }
        #endregion

        #region Test Helpers
        public ConfigurableInfoBar AssertHasAttachedInfoBar(Guid toolwindowGuid)
        {
            ConfigurableInfoBar infoBar = null;
            Assert.IsTrue(this.attached.TryGetValue(toolwindowGuid, out infoBar), "The tool window {0} has no attached info bar", toolwindowGuid);
            return infoBar;
        }

        public void AssertHasNoAttachedInfoBar(Guid toolwindowGuid)
        {
            Assert.IsFalse(this.attached.ContainsKey(toolwindowGuid), "The tool window {0} has attached info bar", toolwindowGuid);
        }
        #endregion
    }
}
