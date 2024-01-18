/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
 * mailto:info AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

using Newtonsoft.Json;

namespace SonarQube.Client.Api.V7_20
{
    internal class GetIssuesWithComponentRequest : GetIssuesRequest
    {
        // this class should not be used on it's own
        private protected GetIssuesWithComponentRequest(){}
        
        public virtual string ComponentKey { get; set; }
    }
    
    /// <summary>
    /// This class is used to override the query string property name for <see cref="ComponentKey"/> for <see cref="ServerType.SonarCloud"/>
    /// </summary>
    /// <remarks>
    /// Reason to create this class https://github.com/SonarSource/sonarlint-visualstudio/issues/5181
    /// </remarks>
    internal class GetIssuesWithComponentSonarCloudRequest : GetIssuesWithComponentRequest, IGetIssuesRequest
    {
        [JsonProperty("componentKeys")]
        public override string ComponentKey { get; set; }
    }

    /// <summary>
    /// This class is used to override the query string property name for <see cref="ComponentKey"/> for <see cref="ServerType.SonarQube"/>
    /// </summary>
    /// <remarks>
    /// Reason to create this class https://github.com/SonarSource/sonarlint-visualstudio/issues/5181
    /// </remarks>
    internal class GetIssuesWithComponentSonarQubeRequest : GetIssuesWithComponentRequest, IGetIssuesRequest
    {
        [JsonProperty("components")]
        public override string ComponentKey { get; set; }
    }
}
