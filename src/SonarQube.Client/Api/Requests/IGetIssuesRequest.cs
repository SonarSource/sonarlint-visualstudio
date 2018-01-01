using SonarQube.Client.Models;

namespace SonarQube.Client.Api
{
    interface IGetIssuesRequest : IRequest<SonarQubeIssue[]>
    {
        string ProjectKey { get; set; }
    }
}
