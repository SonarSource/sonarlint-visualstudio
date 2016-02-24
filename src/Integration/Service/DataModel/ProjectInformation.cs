//-----------------------------------------------------------------------
// <copyright file="ProjectInformation.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Newtonsoft.Json;
using System.Diagnostics;

namespace SonarLint.VisualStudio.Integration.Service
{
    [DebuggerDisplay("Name: {Name}, Key: {Key}")]
    internal class ProjectInformation
    {
        [JsonProperty("k")]
        public string Key { get; set; }

        [JsonProperty("nm")]
        public string Name { get; set; }
    }
}
