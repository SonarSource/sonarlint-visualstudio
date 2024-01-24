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

using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Protocol;

namespace SonarLint.VisualStudio.SLCore.Listener.Credentials
{
    /// <summary>
    /// Credentials provider for SLCore
    /// </summary>
    [Export(typeof(ISLCoreListener))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class CredentialsListener : ISLCoreListener
    {
        public Task<GetCredentialsResponse> GetCredentialsAsync(GetCredentialsParams parameters)
        {
            // stub implementation
            return Task.FromResult(GetCredentialsResponse.NoCredentials);
        }
    }

    internal class GetCredentialsParams
    {
        public GetCredentialsParams(string connectionId)
        {
            this.connectionId = connectionId;
        }

        public string connectionId { get; }
    }

    internal class GetCredentialsResponse
    {
        // credentials property is nullable on the SLCore side
        public static GetCredentialsResponse NoCredentials = new GetCredentialsResponse();
        
        private GetCredentialsResponse(){}
        
        public GetCredentialsResponse(TokenDto token)
        {
            this.credentials = Either<TokenDto, UsernamePasswordDto>.CreateLeft(token);
        }      
        
        public GetCredentialsResponse(UsernamePasswordDto usernamePassword)
        {
            this.credentials = Either<TokenDto, UsernamePasswordDto>.CreateRight(usernamePassword);
        }

        [JsonConverter(typeof(Either<TokenDto, UsernamePasswordDto>))]
        public Either<TokenDto, UsernamePasswordDto> credentials { get; }
    }
}
