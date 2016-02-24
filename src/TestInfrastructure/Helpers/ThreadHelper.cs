//-----------------------------------------------------------------------
// <copyright file="ThreadHelper.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public static class ThreadHelper
    {
        public static void SetCurrentThreadAsUIThread()
        {
            var methodInfo = typeof(Microsoft.VisualStudio.Shell.ThreadHelper).GetMethod("SetUIThread", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            Assert.IsNotNull(methodInfo, "Could not find ThreadHelper.SetUIThread");
            methodInfo.Invoke(null, null);
        }
    }
}
