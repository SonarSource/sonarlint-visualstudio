//-----------------------------------------------------------------------
// <copyright file="CommandHelper.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public static class CommandHelper
    {
        private static Random Random = new Random();

        public static OleMenuCommand CreateRandomOleMenuCommand()
        {
            return new OleMenuCommand((s, e) => { }, new CommandID(Guid.NewGuid(), Random.Next()));
        }
    }
}
