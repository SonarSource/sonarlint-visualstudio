//-----------------------------------------------------------------------
// <copyright file="QualityProfileChangeLogEvent.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Newtonsoft.Json;
using System;

namespace SonarLint.VisualStudio.Integration.Service.DataModel
{
    internal class QualityProfileChangeLogEvent
    {
        [JsonProperty("date")]
        public DateTime Date { get; set; }
    }
}
