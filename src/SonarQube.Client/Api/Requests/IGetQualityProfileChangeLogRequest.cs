using System;

namespace SonarQube.Client.Api.Requests
{
    public interface IGetQualityProfileChangeLogRequest : IPagedRequest<DateTime>
    {
        string QualityProfileKey { get; set; }
    }
}
