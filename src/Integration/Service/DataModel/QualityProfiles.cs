//-----------------------------------------------------------------------
// <copyright file="QualityProfiles.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Newtonsoft.Json;

namespace SonarLint.VisualStudio.Integration.Service.DataModel
{
    internal class QualityProfiles
    {
        [JsonProperty("profiles")]
        public QualityProfile[] Profiles { get; set; }
    }
}
