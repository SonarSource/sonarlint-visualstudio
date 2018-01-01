using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Google.Protobuf;
using Newtonsoft.Json;
using SonarQube.Client.Models;
using SonarQube.Client.Messages;
using SonarQube.Client.Services;

namespace SonarQube.Client.Api.V5_10
{
    public class GetIssuesRequest : RequestBase<SonarQubeIssue[]>, IGetIssuesRequest
    {
        [JsonProperty("key")]
        public string ProjectKey { get; set; }

        protected override string Path => "batch/issues";

        protected async override Task<Result<SonarQubeIssue[]>> ReadResponse(HttpResponseMessage httpResponse)
        {
            if (!httpResponse.IsSuccessStatusCode)
            {
                return new Result<SonarQubeIssue[]>(httpResponse, null);
            }

            var byteArray = await httpResponse.Content.ReadAsByteArrayAsync();
            // Protobuf for C# throws when trying to read outside of the buffer and ReadAsStreamAsync returns a non
            // seekable stream so we can't determine when to stop. The hack is to use an intermediate MemoryStream
            // so we can control when to stop reading.
            // Note we might want to use FileStream instead to avoid intensive memory usage.
            using (var stream = new MemoryStream(byteArray))
            {
                return new Result<SonarQubeIssue[]>(httpResponse,
                    ReadFromProtobufStream(stream, ServerIssue.Parser).Select(ToSonarQubeIssue).ToArray());
            }
        }

        protected override SonarQubeIssue[] ParseResponse(string response)
        {
            throw new NotImplementedException("This method will not be called because we override ReadResponse.");
        }

        private static IEnumerable<T> ReadFromProtobufStream<T>(Stream stream, MessageParser<T> parser)
            where T : IMessage<T>
        {
            while (stream.Position < stream.Length)
            {
                yield return parser.ParseDelimitedFrom(stream);
            }
        }

        private static SonarQubeIssue ToSonarQubeIssue(ServerIssue issue) =>
            new SonarQubeIssue(issue.Path, issue.Checksum, issue.Line, issue.Msg, issue.ModuleKey,
                ParseResolutionState(issue.Resolution), issue.RuleKey);

        public static SonarQubeIssueResolutionState ParseResolutionState(string resolution)
        {
            switch (resolution)
            {
                case "":
                    return SonarQubeIssueResolutionState.Unresolved;
                case "WONTFIX":
                    return SonarQubeIssueResolutionState.WontFix;
                case "FALSE-POSITIVE":
                    return SonarQubeIssueResolutionState.FalsePositive;
                case "FIXED":
                    return SonarQubeIssueResolutionState.Fixed;
                default:
                    throw new ArgumentOutOfRangeException(nameof(resolution));
            }
        }
    }
}
