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
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.Imaging.Interop;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.InfoBar;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    internal class ConfigurableInfoBarManager : IInfoBarManager
    {
        private readonly Dictionary<Guid, ConfigurableInfoBar> attached = new Dictionary<Guid, ConfigurableInfoBar>();

        #region IInfoBarManager

        IInfoBar IInfoBarManager.AttachInfoBarWithButton(Guid toolWindowGuid, string message, string buttonText, SonarLintImageMoniker imageMoniker)
        {
            this.attached.Should().NotContainKey(toolWindowGuid, "Info bar is already attached to tool window {0}", toolWindowGuid);

            var infoBar = new ConfigurableInfoBar(message, buttonText, imageMoniker);
            this.attached[toolWindowGuid] = infoBar;
            return infoBar;
        }

        IInfoBar IInfoBarManager.AttachInfoBar(Guid toolWindowGuid, string message, SonarLintImageMoniker imageMoniker)
        {
            this.attached.Should().NotContainKey(toolWindowGuid, "Info bar is already attached to tool window {0}", toolWindowGuid);

            var infoBar = new ConfigurableInfoBar(message, imageMoniker);
            this.attached[toolWindowGuid] = infoBar;
            return infoBar;
        }

        void IInfoBarManager.DetachInfoBar(IInfoBar currentInfoBar)
        {
            this.attached.Values.Should().Contain((ConfigurableInfoBar)currentInfoBar, "Info bar is not attached");
            this.attached.Remove(attached.Single(kv => kv.Value == currentInfoBar).Key);
        }

        #endregion IInfoBarManager

        #region Test Helpers

        public ConfigurableInfoBar AssertHasAttachedInfoBar(Guid toolwindowGuid)
        {
            this.attached.TryGetValue(toolwindowGuid, out var infoBar).Should().BeTrue("The tool window {0} has no attached info bar", toolwindowGuid);
            return infoBar;
        }

        public void AssertHasNoAttachedInfoBar(Guid toolwindowGuid)
        {
            this.attached.Should().NotContainKey(toolwindowGuid, "The tool window {0} has attached info bar", toolwindowGuid);
        }

        #endregion Test Helpers
    }
}
