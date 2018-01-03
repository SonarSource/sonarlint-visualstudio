using SonarQube.Client.Messages;

namespace SonarQube.Client.Api.Requests
{
    interface IGetRoslynExportProfileRequest : IRequest<RoslynExportProfileResponse>
    {
        string LanguageKey { get; set; }

        string QualityProfileName { get; set; }

        string OrganizationKey { get; set; }
    }
}
