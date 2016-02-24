//-----------------------------------------------------------------------
// <copyright file="ConfigurableErrorNotifier.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarLint.VisualStudio.Progress.Controller.ErrorNotification;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace SonarLint.VisualStudio.Progress.UnitTests
{
    /// <summary>
    /// Test implementation of <see cref="IProgressErrorNotifier"/>
    /// </summary>
    public class ConfigurableErrorNotifier : IProgressErrorNotifier
    {
        public ConfigurableErrorNotifier()
        {
            this.Reset();
        }

        #region Customization properties
        public Action<Exception> NotifyAction
        {
            get;
            set;
        }

        public List<Exception> Exceptions
        {
            get;
            private set;
        }
        #endregion

        #region Customization and verification methods
        public void Reset()
        {
            this.Exceptions = new List<Exception>();
            this.NotifyAction = null;
        }

        public void AssertNoExceptions()
        {
            Assert.AreEqual(0, this.Exceptions.Count, "Not expecting any errors");
        }

        public void AssertExcepections(int expectedNumberOfExceptions)
        {
            Assert.AreEqual(expectedNumberOfExceptions, this.Exceptions.Count, "Unexpected number of exceptions");
        }
        #endregion

        #region Test implementation of IProgressErrorHandler (not to be used explicitly by the test code)
        void IProgressErrorNotifier.Notify(Exception ex)
        {
            this.Exceptions.Add(ex);
            if (this.NotifyAction != null)
            {
                this.NotifyAction(ex);
            }
        }
        #endregion
    }
}
