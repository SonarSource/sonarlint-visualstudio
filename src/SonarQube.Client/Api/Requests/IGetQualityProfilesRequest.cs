using SonarQube.Client.Models;

namespace SonarQube.Client.Api.Requests
{
    public interface IGetQualityProfilesRequest : IRequest<SonarQubeQualityProfile[]>
    {
        string ProjectKey { get; set; }

        string OrganizationKey { get; set; }
    }
}
