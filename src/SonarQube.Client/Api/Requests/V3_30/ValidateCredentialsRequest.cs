using Newtonsoft.Json.Linq;

namespace SonarQube.Client.Api.Requests.V3_30
{
    public class ValidateCredentialsRequest : RequestBase<bool>, IValidateCredentialsRequest
    {
        protected override string Path => "api/authentication/validate";

        protected override bool ParseResponse(string response) =>
            (bool)JObject.Parse(response).SelectToken("valid");
    }
}
