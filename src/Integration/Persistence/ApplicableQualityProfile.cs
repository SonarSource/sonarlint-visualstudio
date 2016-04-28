//-----------------------------------------------------------------------
// <copyright file="ApplicableQualityProfile.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using System;

namespace SonarLint.VisualStudio.Integration.Persistence
{
    internal class ApplicableQualityProfile
    {
        public string ProfileKey { get; set; }

        public DateTime? ProfileTimestamp { get; set; }
    }
}
