﻿/*
 * SonarQube Client
 * Copyright (C) 2016-2017 SonarSource SA
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

using System;
using System.Security;
using SonarQube.Client.Helpers;

namespace SonarQube.Client.Models
{
    /// <summary>
    /// Represents the connection information needed to connect to SonarQube service
    /// </summary>
    public sealed class ConnectionInformation : ICloneable, IDisposable
    {
        private bool isDisposed;

        public ConnectionInformation(Uri serverUri, string userName, SecureString password)
        {
            if (serverUri == null)
            {
                throw new ArgumentNullException(nameof(serverUri));
            }

            this.ServerUri = serverUri.EnsureTrailingSlash();
            this.UserName = userName;
            this.Password = password?.CopyAsReadOnly();
            this.Authentication = AuthenticationType.Basic; // Only one supported at this point
        }

        public ConnectionInformation(Uri serverUri)
            : this(serverUri, null, null)
        {
        }

        public Uri ServerUri
        {
            get;
        }

        public string UserName
        {
            get;
        }

        public SecureString Password
        {
            get;
        }

        public AuthenticationType Authentication
        {
            get;
        }

        public bool IsDisposed => this.isDisposed;

        public Organization Organization { get; set; }

        public ConnectionInformation Clone()
        {
            return new ConnectionInformation(this.ServerUri, this.UserName, this.Password?.CopyAsReadOnly()) { Organization = Organization };
        }

        object ICloneable.Clone()
        {
            return this.Clone();
        }

        #region IDisposable Support

        public void Dispose()
        {
            if (!this.isDisposed)
            {
                this.Password?.Dispose();
                this.isDisposed = true;
            }
        }
        #endregion
    }

    public class ConnectionDTO
    {
        public Uri ServerUri { get; set; }
        public string Login { get; set; }
        public SecureString Password { get; set; }
        public AuthenticationType Authentication { get; set; }
    }
}
