namespace SonarQube.Client.Api
{
    public interface IPagedRequest<TResponseItem> : IRequest<TResponseItem[]>
    {
        int Page { get; set; }

        int PageSize { get; set; }
    }
}
