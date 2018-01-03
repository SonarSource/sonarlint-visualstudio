using System;
using System.Net.Http;
using System.Threading;
using SonarQube.Client.Api.Requests;
using SonarQube.Client.Services;

namespace SonarQube.Client.RequestGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            var requestFactory = new RequestFactory();
            DefaultConfiguration.Configure(requestFactory);

            var cts = new CancellationTokenSource();

            var service = new SonarQubeService2(new HttpClientHandler(), requestFactory);
            //var service = new SonarQubeService(new SonarQubeClientFactory());

            var parser = new ArgsParser(args);

            var runner = new ServiceRunner(service)
            {
                OutputPath = parser.NextArg(),
                SonarQubeUrl = new Uri(parser.NextArg()),
                Username = parser.NextArg(),
                Password = parser.NextArg(),
                Project = parser.NextArg(),
                Organization = parser.NextArg(),
                RoslynQualityProfile = parser.NextArg(),
            };

            runner.Run(args, cts.Token)
                .GetAwaiter()
                .GetResult();
        }
    }
}
