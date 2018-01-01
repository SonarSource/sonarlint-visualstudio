using System;
using SonarQube.Client.Models;

namespace SonarQube.Client.Api.Requests
{
    public interface IGetNotificationsRequest : IRequest<SonarQubeNotification[]>
    {
        string ProjectKey { get; set; }

        DateTimeOffset EventsSince { get; set; }
    }
}
