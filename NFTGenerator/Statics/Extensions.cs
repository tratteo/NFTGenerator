// Copyright Matteo Beltrame

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using NFTGenerator.Services;
using System.Threading;
using System.Threading.Tasks;

namespace NFTGenerator.Statics;

public static class Extensions
{
    public static float Remap(this float from, float fromMin, float fromMax, float toMin, float toMax)
    {
        var fromAbs = from - fromMin;
        var fromMaxAbs = fromMax - fromMin;

        var normal = fromAbs / fromMaxAbs;

        var toMaxAbs = toMax - toMin;
        var toAbs = toMaxAbs * normal;

        var to = toAbs + toMin;

        return to;
    }

    public static double Remap(this double from, double fromMin, double fromMax, double toMin, double toMax)
    {
        var fromAbs = from - fromMin;
        var fromMaxAbs = fromMax - fromMin;

        var normal = fromAbs / fromMaxAbs;

        var toMaxAbs = toMax - toMin;
        var toAbs = toMaxAbs * normal;

        var to = toAbs + toMin;

        return to;
    }

    public static void AddOfflineRunner<T>(this IServiceCollection services) where T : class, IRunner
    {
        services.AddSingleton<IRunner, T>();
    }

    public static Task RunOfflineAsync(this WebApplication app, CancellationToken token)
    {
        var runner = app.Services.GetRequiredService<IRunner>();
        return runner.RunAsync(token);
    }
}