using SonarQube.Client.Models;

namespace SonarQube.Client.Api.Requests
{
    public interface IGetPropertiesRequest : IRequest<SonarQubeProperty[]>
    {
    }
}
