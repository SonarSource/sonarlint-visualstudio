using System.Diagnostics;

namespace SonarQube.Client.Helpers
{
    public static class FilePathNormalizer
    {
        /// <summary>
        /// Converts SQ file path format into Windows file path format.
        /// </summary>
        /// <remarks>
        /// Forward-slashes are replaced with back-slashes.
        /// Opening slashes are removed.
        /// </remarks>
        public static string NormalizeSonarQubePath(string path)
        {
            Debug.Assert(path == null || !path.Contains("\\"),
                $"Expecting sonarqube relative path delimiters to be forward-slash but got '{path}'.");

            return path?.Trim('/').Replace('/', '\\')
                   ?? string.Empty;
        }
    }
}
