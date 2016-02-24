using System;
using System.Security;

namespace Microsoft.Alm.Authentication
{
    /// <summary>
    /// Credentials for user authentication.
    /// </summary>
    public sealed class Credential : Secret
    {
        /// <summary>
        /// Creates a credential object with a username and password pair.
        /// </summary>
        /// <param name="username">The username value of the <see cref="Credential"/>.</param>
        /// <param name="password">The password value of the <see cref="Credential"/>.</param>
        public Credential(string username, SecureString password)
        {
            this.Username = username;
            this.Password = password;
        }

        /// <summary>
        /// Secret related to the username.
        /// </summary>
        public readonly SecureString Password;
        /// <summary>
        /// Unique identifier of the user.
        /// </summary>
        public readonly string Username;

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal static void Validate(Credential credentials)
        {
            if (credentials == null)
                throw new ArgumentNullException(nameof(credentials));
            if (credentials.Password.Length > NativeMethods.Credential.PasswordMaxLength)
                throw new ArgumentOutOfRangeException(nameof(credentials.Password));
            if (credentials.Username.Length > NativeMethods.Credential.UsernameMaxLength)
                throw new ArgumentOutOfRangeException(nameof(credentials.Username));
        }
    }
}

