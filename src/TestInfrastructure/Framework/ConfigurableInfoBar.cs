//-----------------------------------------------------------------------
// <copyright file="ConfigurableInfoBar.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.InfoBar;
using System;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    internal class ConfigurableInfoBar : IInfoBar
    {
        private int closedCalled;

        public ConfigurableInfoBar(string message, string buttonText, ImageMoniker imageMoniker)
        {
            Assert.IsNotNull(message, "Message is null");
            Assert.IsNotNull(buttonText, "Button text is null");
            Assert.IsNotNull(imageMoniker, "image moniker is null");

            this.Message = message;
            this.ButtonText = buttonText;
            this.Image = imageMoniker;
        }

        #region IInfoBar
        public event EventHandler ButtonClick;
        public event EventHandler Closed;

        public void Close()
        {
            this.closedCalled++;
        }
        #endregion

        #region Test helpers
        public string Message { get; }
        public string ButtonText { get; }
        public ImageMoniker Image { get; }

        public void AssertClosedCalled(int expectedNumberOfTimes)
        {
            Assert.AreEqual(expectedNumberOfTimes, this.closedCalled, $"{nameof(Close)} was called unexpected number of times");
        }

        public void SimulatButtonClickEvent()
        {
            this.ButtonClick?.Invoke(this, EventArgs.Empty);
        }

        public void SimulatClosedEvent()
        {
            this.Closed?.Invoke(this, EventArgs.Empty);
        }

        public void VerifyAllEventsUnregistered()
        {
            Assert.IsNull(this.ButtonClick, $"{nameof(this.ButtonClick)} event was remained registered");
            Assert.IsNull(this.Closed, $"{nameof(this.Closed)} event was remained registered");
        }

        public void VerifyAllEventsRegistered()
        {
            Assert.IsNotNull(this.ButtonClick, $"{nameof(this.ButtonClick)} event is not registered");
            Assert.IsNotNull(this.Closed, $"{nameof(this.Closed)} event is not registered");
        }
        #endregion
    }
}
