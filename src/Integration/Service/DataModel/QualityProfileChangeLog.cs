//-----------------------------------------------------------------------
// <copyright file="QualityProfileChangeLog.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Newtonsoft.Json;
using System.Collections.Generic;

namespace SonarLint.VisualStudio.Integration.Service.DataModel
{
    internal class QualityProfileChangeLog
    {
        [JsonProperty("total")]
        public int Total { get; set; }

        [JsonProperty("ps")]
        public int PageSize { get; set; }

        [JsonProperty("p")]
        public int Page { get; set; }

        [JsonProperty("events")]
        public QualityProfileChangeLogEvent[] Events { get; set; }
    }
}
