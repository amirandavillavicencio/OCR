using AppPortable.Core;

namespace AppPortable.Infrastructure;

public static class InfrastructureService
{
    public static string GetInfrastructureInfo() => $"Infrastructure using Core {CoreService.GetVersion()}";
}
