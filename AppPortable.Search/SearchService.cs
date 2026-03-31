using AppPortable.Infrastructure;

namespace AppPortable.Search;

public static class SearchService
{
    public static string Status() => InfrastructureService.GetInfrastructureInfo();
}
