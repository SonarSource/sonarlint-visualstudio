using System;
using System.Security;

namespace SonarQube.Client.Models
{
    public class Connection
    {
        public Uri ServerUri { get; set; }
        public string Login { get; set; }
        public SecureString Password { get; set; }
        public AuthenticationType Authentication { get; set; }
    }

    public class ConnectionDTO
    {
        public Uri ServerUri { get; set; }
        public string Login { get; set; }
        public SecureString Password { get; set; }
        public AuthenticationType Authentication { get; set; }
    }
}
