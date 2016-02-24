//-----------------------------------------------------------------------
// <copyright file="MessageVerificationHelper.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Globalization;

namespace SonarLint.VisualStudio.Progress.UnitTests
{
    /// <summary>
    /// Test helper class that verifies the message formating
    /// </summary>
    public static class MessageVerificationHelper
    {
        public static void VerifyNotificationMessage(string actualMessage, string expectedFormat, Exception ex, bool logWholeMessage)
        {
            Assert.AreEqual(string.Format(CultureInfo.CurrentCulture, expectedFormat, logWholeMessage ? ex.ToString() : ex.Message), actualMessage, "Unexpected error message");
        }
    }
}
