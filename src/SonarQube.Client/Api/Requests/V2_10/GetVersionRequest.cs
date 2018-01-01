namespace SonarQube.Client.Api.Requests.V2_10
{
    public class GetVersionRequest : RequestBase<string>, IGetVersionRequest
    {
        protected override string Path => "api/server/version";

        protected override string ParseResponse(string response) => response;
    }
}
