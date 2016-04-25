//-----------------------------------------------------------------------
// <copyright file="ServerPlugin.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Newtonsoft.Json;
using System.Diagnostics;

namespace SonarLint.VisualStudio.Integration.Service.DataModel
{
    [DebuggerDisplay("Key: {Key}, Version: {Version}")]
    internal class ServerPlugin
    {
        #region Known plugins

        public const string CSharpPluginKey = "csharp";
        public const string CSharpPluginMinimumVersion = "5.0";

        #endregion

        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }
    }
}