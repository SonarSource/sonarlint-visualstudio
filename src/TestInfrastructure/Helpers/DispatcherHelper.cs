//-----------------------------------------------------------------------
// <copyright file="DispatcherHelper.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using System.Windows.Threading;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public static class DispatcherHelper
    {
        public static void DispatchFrame(DispatcherPriority priority = DispatcherPriority.Background)
        {
            DispatcherFrame frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(priority,
                new DispatcherOperationCallback((f)=>((DispatcherFrame)f).Continue = false), frame);
            Dispatcher.PushFrame(frame);
        }
    }
}
