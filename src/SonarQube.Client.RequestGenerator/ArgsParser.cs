using System.Linq;

namespace SonarQube.Client.RequestGenerator
{
    public class ArgsParser
    {
        private readonly string[] args;
        private readonly string[] defaults = new[]
        {
                ".", // output path
                "http://localhost:9000", // sq url
                "admin", // user
                "admin", // pass
                "test1", // project name
                null, // organization name
                "Sonar Way" // quality profile
            };

        private int index;

        public ArgsParser(string[] args)
        {
            this.args = args;
            this.index = 0;
        }

        public string NextArg()
        {
            var arg = args.ElementAtOrDefault(index)
                ?? defaults.ElementAtOrDefault(index);
            index++;
            return arg;
        }
    }
}
