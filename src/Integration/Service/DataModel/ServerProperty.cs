//-----------------------------------------------------------------------
// <copyright file="ServerProperty.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Newtonsoft.Json;
using System.Diagnostics;

namespace SonarLint.VisualStudio.Integration.Service
{
    [DebuggerDisplay("Key = {key}, Value = {value}")]
    internal class ServerProperty
    {
        #region Known properties

        public const string TestProjectRegexKey = "sonar.cs.msbuild.testProjectPattern";
        public const string TestProjectRegexDefaultValue = @"[^\\]*test[^\\]*$";

        #endregion

        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }
    }
}
