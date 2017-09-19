using System;

namespace SonarQube.Client.Helpers
{
    public static class UriHelper
    {
        public static Uri EnsureTrailingSlash(this Uri uri)
        {
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            var uriString = uri.ToString();

            if (!uriString.EndsWith("/"))
            {
                return new Uri(uriString + "/");
            }

            return uri;
        }
    }
}
