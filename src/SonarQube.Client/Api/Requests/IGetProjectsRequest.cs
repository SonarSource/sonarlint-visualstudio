using SonarQube.Client.Models;

namespace SonarQube.Client.Api.Requests
{
    public interface IGetProjectsRequest : IPagedRequest<SonarQubeProject>
    {
        string OrganizationKey { get; set; }

        bool Ascending { get; set; }
    }
}
