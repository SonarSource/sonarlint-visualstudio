/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
 * mailto: contact AT sonarsource DOT com
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

namespace Microsoft.Alm.Authentication
{
    public enum TokenType
    {
        Unknown = 0,
        /// <summary>
        /// Azure Directory Access Token
        /// </summary>
        [System.ComponentModel.Description("Azure Directory Access Token")]
        Access,
        /// <summary>
        /// Azure Directory Refresh Token
        /// </summary>
        [System.ComponentModel.Description("Azure Directory Refresh Token")]
        Refresh,
        /// <summary>
        /// Personal Access Token, can be compact or not.
        /// </summary>
        [System.ComponentModel.Description("Personal Access Token")]
        Personal,
        /// <summary>
        /// Federated Authentication (aka FedAuth) Token
        /// </summary>
        [System.ComponentModel.Description("Federated Authentication Token")]
        Federated,
        /// <summary>
        /// Used only for testing
        /// </summary>
        [System.ComponentModel.Description("Test-only Token")]
        Test,
    }
}

