namespace SonarQube.Client.Api.Requests
{
    public static class DefaultConfiguration
    {
        public static RequestFactory Configure(RequestFactory requestFactory)
        {
            requestFactory
                .RegisterRequest<IGetPluginsRequest, V2_10.GetPluginsRequest>("2.1")
                .RegisterRequest<IGetProjectsRequest, V2_10.GetProjectsRequest>("2.1")
                .RegisterRequest<IGetVersionRequest, V2_10.GetVersionRequest>("2.1")
                .RegisterRequest<IGetPropertiesRequest, V2_60.GetPropertiesRequest>("2.6")
                .RegisterRequest<IValidateCredentialsRequest, V3_30.ValidateCredentialsRequest>("3.3")
                .RegisterRequest<IGetIssuesRequest, V5_10.GetIssuesRequest>("5.1")
                .RegisterRequest<IGetQualityProfileChangeLogRequest, V5_20.GetQualityProfileChangeLogRequest>("5.2")
                .RegisterRequest<IGetQualityProfilesRequest, V5_20.GetQualityProfilesRequest>("5.2")
                .RegisterRequest<IGetRoslynExportProfileRequest, V5_20.GetRoslynExportProfileRequest>("5.2")
                .RegisterRequest<IGetProjectsRequest, V6_20.GetProjectsRequest>("6.2")
                .RegisterRequest<IGetOrganizationsRequest, V6_20.GetOrganizationsRequest>("6.2")
                .RegisterRequest<IGetPropertiesRequest, V6_30.GetPropertiesRequest>("6.3")
                .RegisterRequest<IGetQualityProfilesRequest, V6_50.GetQualityProfilesRequest>("6.5")
                .RegisterRequest<IGetNotificationsRequest, V6_60.GetNotificationsRequest>("6.6")
                .RegisterRequest<IGetRoslynExportProfileRequest, V6_60.GetRoslynExportProfileRequest>("6.6");
            return requestFactory;
        }
    }
}
