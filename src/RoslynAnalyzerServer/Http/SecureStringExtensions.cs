using System.Runtime.InteropServices;
using System.Security;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.Http;

internal static class SecureStringExtensions
{
    internal static SecureString ToSecureString(this string str)
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

    // Copied from http://blogs.msdn.com/b/fpintos/archive/2009/06/12/how-to-properly-convert-securestring-to-string.aspx
    /// <summary>
    /// WARNING: This will create plain text <see cref="string"/> version of the <see cref="SecureString"/> in
    /// memory which is not encrypted. This could lead to leaking of sensitive information and other security
    /// vulnerabilities - heavy caution is advised.
    /// </summary>
    [SecurityCritical]
    public static string ToUnsecureString(this SecureString secureString)
    {
        if (secureString == null)
        {
            throw new ArgumentNullException(nameof(secureString));
        }

        IntPtr unmanagedString = IntPtr.Zero;
        try
        {
            unmanagedString = Marshal.SecureStringToGlobalAllocUnicode(secureString);
            return Marshal.PtrToStringUni(unmanagedString);
        }
        finally
        {
            Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
        }
    }
}
