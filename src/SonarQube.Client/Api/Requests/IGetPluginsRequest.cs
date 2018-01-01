using SonarQube.Client.Models;

namespace SonarQube.Client.Api.Requests
{
    public interface IGetPluginsRequest : IRequest<SonarQubePlugin[]>
    {
    }
}
