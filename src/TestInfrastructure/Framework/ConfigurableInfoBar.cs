﻿/*
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
using FluentAssertions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.InfoBar;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    internal class ConfigurableInfoBar : IInfoBar
    {
        internal int ClosedCalledCount { get; private set; }

        public ConfigurableInfoBar(string message, string buttonText, SonarLintImageMoniker imageMoniker)
        {
            message.Should().NotBeNull("Message is null");
            buttonText.Should().NotBeNull("Button text is null");
            imageMoniker.Should().NotBeNull("image moniker is null");

            this.Message = message;
            this.ButtonText = buttonText;
            this.Image = imageMoniker;
        }

        public ConfigurableInfoBar(string message, SonarLintImageMoniker imageMoniker)
        {
            message.Should().NotBeNull("Message is null");
            imageMoniker.Should().NotBeNull("image moniker is null");

            this.Message = message;
            this.ButtonText = null;
            this.Image = imageMoniker;
        }

        #region IInfoBar

        public event EventHandler<InfoBarButtonClickedEventArgs> ButtonClick;

        public event EventHandler Closed;

        public void Close()
        {
            this.ClosedCalledCount++;
        }

        #endregion IInfoBar

        #region Test helpers

        public string Message { get; }
        public string ButtonText { get; }
        public SonarLintImageMoniker Image { get; }

        public void SimulateButtonClickEvent()
        {
            this.ButtonClick?.Invoke(this, new InfoBarButtonClickedEventArgs(ButtonText));
        }

        public void SimulateClosedEvent()
        {
            this.Closed?.Invoke(this, EventArgs.Empty);
        }

        public void VerifyAllEventsUnregistered()
        {
            this.ButtonClick.Should().BeNull($"{nameof(this.ButtonClick)} event remained registered");
            this.Closed.Should().BeNull($"{nameof(this.Closed)} event remained registered");
        }

        public void VerifyAllEventsRegistered()
        {
            this.ButtonClick.Should().NotBeNull($"{nameof(this.ButtonClick)} event is not registered");
            this.Closed.Should().NotBeNull($"{nameof(this.Closed)} event is not registered");
        }

        #endregion Test helpers
    }
}
