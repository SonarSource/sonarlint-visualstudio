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
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace Microsoft.Alm.Authentication
{
    public abstract class BaseSecureStore
    {
        public static readonly char[] IllegalCharacters = new[] { ':', ';', '\\', '?', '@', '=', '&', '%', '$' };

        protected void Delete(string targetName)
        {
            Trace.WriteLine("BaseSecureStore::Delete");

            try
            {
                if (NativeMethods.CredDelete(targetName, NativeMethods.CredentialType.Generic, 0))
                {
                    return;
                }


                int error = Marshal.GetLastWin32Error();
                switch (error)
                {
                    case NativeMethods.Win32Error.NotFound:
                    case NativeMethods.Win32Error.NoSuchLogonSession:
                        Trace.WriteLine("   credentials not found for " + targetName);
                        break;

                    default:
                        throw new Win32Exception(error, "Failed to delete credentials for " + targetName);
                }
            }
            catch (Exception ex)
            {
                if (ErrorHandler.IsCriticalException(ex))
                {
                    throw;
                }
                Debug.WriteLine(ex);
            }
        }

        protected abstract string GetTargetName(Uri targetUri);

        protected Credential ReadCredentials(string targetName)
        {
            Trace.WriteLine("BaseSecureStore::ReadCredentials");

            Credential credentials = null;
            IntPtr credPtr = IntPtr.Zero;

            try
            {
                if (!NativeMethods.CredRead(targetName, NativeMethods.CredentialType.Generic, 0, out credPtr))
                {
                    return null;
                }

                NativeMethods.Credential credStruct = (NativeMethods.Credential)Marshal.PtrToStructure(credPtr, typeof(NativeMethods.Credential));

                // https://msdn.microsoft.com/en-us/library/gg309393.aspx
                int size = (int)credStruct.CredentialBlobSize;
                SecureString pwd = null;
                if (size != 0)
                {
                    byte[] bpassword = new byte[size];
                    Marshal.Copy(credStruct.CredentialBlob, bpassword, 0, size);
                    char[] chars = Encoding.Unicode.GetChars(bpassword);
                    pwd = ConvertToSecureString(chars);
                    Array.Clear(chars, 0, chars.Length);
                    Array.Clear(bpassword, 0, bpassword.Length);
                }

                credentials = new Credential(credStruct.UserName, pwd);
            }
            finally
            {
                if (credPtr != IntPtr.Zero)
                {
                    NativeMethods.CredFree(credPtr);
                }
            }

            return credentials;
        }

        protected Token ReadToken(string targetName)
        {
            Trace.WriteLine("BaseSecureStore::ReadToken");

            Token token = null;
            IntPtr credPtr = IntPtr.Zero;

            try
            {
                if (!NativeMethods.CredRead(targetName, NativeMethods.CredentialType.Generic, 0, out credPtr))
                {
                    return null;
                }

                NativeMethods.Credential credStruct = (NativeMethods.Credential)Marshal.PtrToStructure(credPtr, typeof(NativeMethods.Credential));
                if (credStruct.CredentialBlob == null || credStruct.CredentialBlobSize <= 0)
                {
                    return null;
                }

                int size = (int)credStruct.CredentialBlobSize;
                byte[] bytes = new byte[size];
                Marshal.Copy(credStruct.CredentialBlob, bytes, 0, size);

                TokenType type;
                if (Token.GetTypeFromFriendlyName(credStruct.UserName, out type))
                {
                    Token.Deserialize(bytes, type, out token);
                }
            }
            finally
            {
                if (credPtr != IntPtr.Zero)
                {
                    NativeMethods.CredFree(credPtr);
                }
            }

            return token;
        }

        protected void WriteCredential(string targetName, Credential credentials)
        {
            Trace.WriteLine("BaseSecureStore::WriteCredential");

            NativeMethods.Credential credential = new NativeMethods.Credential()
            {
                Type = NativeMethods.CredentialType.Generic,
                TargetName = targetName,
                Persist = NativeMethods.CredentialPersist.LocalMachine,
                AttributeCount = 0,
                UserName = credentials.Username,
            };
            try
            {
                // https://msdn.microsoft.com/en-us/library/gg309393.aspx
                credential.CredentialBlob = Marshal.SecureStringToCoTaskMemUnicode(credentials.Password);
                // See calculation in http://referencesource.microsoft.com/#mscorlib/system/security/securestring.cs,eab10308ba549df3
                credential.CredentialBlobSize = (uint)(credentials.Password.Length + 1 /*null terminator*/) * 2 /*Unicode encoding*/;

                if (!NativeMethods.CredWrite(ref credential, 0))
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    Trace.WriteLine("BaseSecureStore::WriteCredential Failed to write credentials, error code " + errorCode);
                }
            }
            finally
            {
                if (credential.CredentialBlob != IntPtr.Zero)
                {
                    Marshal.ZeroFreeCoTaskMemUnicode(credential.CredentialBlob);
                }
            }
        }

        protected void WriteToken(string targetName, Token token)
        {
            Trace.WriteLine("BaseSecureStore::WriteToken");

            byte[] bytes = null;
            if (!Token.Serialize(token, out bytes))
            {
                return;
            }

            string name;
            if (!Token.GetFriendlyNameFromType(token.Type, out name))
            {
                return;
            }

            NativeMethods.Credential credential = new NativeMethods.Credential()
            {
                Type = NativeMethods.CredentialType.Generic,
                TargetName = targetName,
                CredentialBlobSize = (uint)bytes.Length,
                Persist = NativeMethods.CredentialPersist.LocalMachine,
                AttributeCount = 0,
                UserName = name,
            };
            try
            {
                credential.CredentialBlob = Marshal.AllocCoTaskMem(bytes.Length);
                Marshal.Copy(bytes, 0, credential.CredentialBlob, bytes.Length);

                if (!NativeMethods.CredWrite(ref credential, 0))
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    Trace.WriteLine("BaseSecureStore::WriteToken Failed to write credentials, error code " + errorCode);
                }
            }
            finally
            {
                if (credential.CredentialBlob != IntPtr.Zero)
                {
                    Marshal.ZeroFreeCoTaskMemUnicode(credential.CredentialBlob);
                }
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal static void ValidateTargetUri(Uri targetUri)
        {
            if (targetUri == null)
            {
                throw new ArgumentNullException(nameof(targetUri));
            }

            if (!targetUri.IsAbsoluteUri)
            {
                throw new ArgumentException(Strings.ExpectedAbsoluteUris, nameof(targetUri));
            }
        }

        private static SecureString ConvertToSecureString(char[] str)
        {
            if (str == null)
            {
                throw new ArgumentNullException(nameof(str));
            }

            var secure = new SecureString();
            foreach (char c in str)
            {
                secure.AppendChar(c);
            }
            secure.MakeReadOnly();
            return secure;
        }

    }
}

