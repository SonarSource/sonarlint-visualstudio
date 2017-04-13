/*
 * SonarLint for Visual Studio
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

using Microsoft.VisualStudio;
using SonarLint.VisualStudio.Integration.Resources;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Microsoft.Alm.Authentication
{
    /// <summary>
    /// A security token, usually acquired by some authentication and identity services.
    /// </summary>
    public class Token : Secret, IEquatable<Token>
    {
        public static bool GetFriendlyNameFromType(TokenType type, out string name)
        {
            if (!Enum.IsDefined(typeof(TokenType), type))
            {
                throw new ArgumentOutOfRangeException(nameof(type));
            }

            name = null;

            System.ComponentModel.DescriptionAttribute attribute = type.GetType()
                                                                       .GetField(type.ToString())
                                                                       .GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), false)
                                                                       .SingleOrDefault() as System.ComponentModel.DescriptionAttribute;
            name = attribute == null
                ? type.ToString()
                : attribute.Description;

            return name != null;
        }

        public static bool GetTypeFromFriendlyName(string name, out TokenType type)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            type = TokenType.Unknown;

            foreach (var value in Enum.GetValues(typeof(TokenType)))
            {
                type = (TokenType)value;

                string typename;
                if (GetFriendlyNameFromType(type, out typename) && string.Equals(name, typename, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        internal Token(string value, TokenType type)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(value), "The value parameter is null or invalid");
            Debug.Assert(Enum.IsDefined(typeof(TokenType), type), "The type parameter is invalid");

            this.Type = type;
            this.Value = value;
        }
        internal Token(string value, string typeName)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(value), "The value parameter is null or invalid");
            Debug.Assert(!string.IsNullOrWhiteSpace(typeName), "The typeName parameter is null or invalid");

            TokenType type;
            if (!GetTypeFromFriendlyName(typeName, out type))
            {
                throw new ArgumentException(Strings.UnexpectedTokenType, nameof(typeName));
            }
        }

        internal Token(IdentityModel.Clients.ActiveDirectory.AuthenticationResult authResult, TokenType type)
        {
            Debug.Assert(authResult != null, "The authResult parameter is null");
            Debug.Assert(!string.IsNullOrWhiteSpace(authResult.AccessToken), "The authResult.AccessToken parameter is null or invalid.");
            Debug.Assert(!string.IsNullOrWhiteSpace(authResult.RefreshToken), "The authResult.RefreshToken parameter is null or invalid.");
            Debug.Assert(Enum.IsDefined(typeof(TokenType), type), "The type parameter is invalid");

            switch (type)
            {
                case TokenType.Access:
                    this.Value = authResult.AccessToken;
                    break;

                case TokenType.Refresh:
                    this.Value = authResult.RefreshToken;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }

            Guid tenantId;
            if (Guid.TryParse(authResult.TenantId, out tenantId))
            {
                this.TargetIdentity = tenantId;
            }
            this.Type = type;
        }

        /// <summary>
        /// The type of the security token.
        /// </summary>
        public readonly TokenType Type;
        /// <summary>
        /// The raw contents of the token.
        /// </summary>
        public readonly string Value;
        /// <summary>
        /// The GUID form Identity of the target
        /// </summary>
        public Guid TargetIdentity { get; internal set; }

        /// <summary>
        /// Compares an object to this <see cref="Token"/> for equality.
        /// </summary>
        /// <param name="obj">The object to compare.</param>
        /// <returns>True is equal; false otherwise.</returns>
        public override bool Equals(object obj)
        {
            return this.Equals(obj as Token);
        }
        /// <summary>
        /// Compares a <see cref="Token"/> to this Token for equality.
        /// </summary>
        /// <param name="other">The token to compare.</param>
        /// <returns>True if equal; false otherwise.</returns>
        public bool Equals(Token other)
        {
            return this == other;
        }
        /// <summary>
        /// Gets a hash code based on the contents of the token.
        /// </summary>
        /// <returns>32-bit hash code.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)Type) * Value.GetHashCode();
            }
        }
        /// <summary>
        /// Converts the token to a human friendly string.
        /// </summary>
        /// <returns>Humanized name of the token.</returns>
        public override string ToString()
        {
            string value;
            if (GetFriendlyNameFromType(Type, out value))
            {
                return value;
            }

            return base.ToString();
        }

        internal static unsafe bool Deserialize(byte[] bytes, TokenType type, out Token token)
        {
            Debug.Assert(bytes != null, "The bytes parameter is null");
            Debug.Assert(bytes.Length > 0, "The bytes parameter is too short");
            Debug.Assert(Enum.IsDefined(typeof(TokenType), type), "The type parameter is invalid");

            token = null;
            try
            {
                token = TryDeserializeWithNewFormat(bytes, type);

                // if value hasn't been set yet, fall back to old format decode
                if (token == null)
                {
                    string value = Encoding.UTF8.GetString(bytes);

                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        token = new Token(value, type);
                    }
                }
            }
            catch (Exception ex)
            {
                if (ErrorHandler.IsCriticalException(ex))
                {
                    throw;
                }

                Trace.WriteLine("   token deserialization error");
            }

            return token != null;
        }

        private static unsafe Token TryDeserializeWithNewFormat(byte[] bytes, TokenType type)
        {
            int preamble = sizeof(TokenType) + sizeof(Guid);

            if (bytes.Length <= preamble)
            {
                return null;
            }


            TokenType readType;
            Guid targetIdentity;

            fixed (byte* p = bytes)
            {
                readType = *(TokenType*)p;
                byte* g = p + sizeof(TokenType);
                targetIdentity = *(Guid*)g;
            }

            if (readType == type)
            {
                string value = Encoding.UTF8.GetString(bytes, preamble, bytes.Length - preamble);

                if (!string.IsNullOrWhiteSpace(value))
                {
                    var token = new Token(value, type);
                    token.TargetIdentity = targetIdentity;
                    return token;
                }
            }

            return null;
        }

        internal static unsafe bool Serialize(Token token, out byte[] bytes)
        {
            Debug.Assert(token != null, "The token parameter is null");
            Debug.Assert(!string.IsNullOrWhiteSpace(token.Value), "The token.Value is invalid");

            bytes = null;

            try
            {
                byte[] utf8bytes = Encoding.UTF8.GetBytes(token.Value);
                bytes = new byte[utf8bytes.Length + sizeof(TokenType) + sizeof(Guid)];

                fixed (byte* p = bytes)
                {
                    *((TokenType*)p) = token.Type;
                    byte* g = p + sizeof(TokenType);
                    *(Guid*)g = token.TargetIdentity;
                }

                Array.Copy(utf8bytes, 0, bytes, sizeof(TokenType) + sizeof(Guid), utf8bytes.Length);
            }
            catch (Exception ex)
            {
                if (ErrorHandler.IsCriticalException(ex))
                {
                    throw;
                }
                Trace.WriteLine("   token serialization error");
            }

            return bytes != null;
        }

        internal static void Validate(Token token)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            if (string.IsNullOrWhiteSpace(token.Value))
            {
                throw new ArgumentException(nameof(token));
            }

            if (token.Value.Length > NativeMethods.Credential.PasswordMaxLength)
            {
                throw new ArgumentOutOfRangeException(nameof(token.Value));
            }
        }

        /// <summary>
        /// Compares two tokens for equality.
        /// </summary>
        /// <param name="token1">Token to compare.</param>
        /// <param name="token2">Token to compare.</param>
        /// <returns>True if equal; false otherwise.</returns>
        public static bool operator ==(Token token1, Token token2)
        {
            if (ReferenceEquals(token1, token2))
            {
                return true;
            }

            if (ReferenceEquals(token1, null) || ReferenceEquals(null, token2))
            {
                return false;
            }

            return token1.Type == token2.Type
                && string.Equals(token1.Value, token2.Value, StringComparison.Ordinal);
        }
        /// <summary>
        /// Compares two tokens for inequality.
        /// </summary>
        /// <param name="token1">Token to compare.</param>
        /// <param name="token2">Token to compare.</param>
        /// <returns>False if equal; true otherwise.</returns>
        public static bool operator !=(Token token1, Token token2)
        {
            return !(token1 == token2);
        }
    }
}

