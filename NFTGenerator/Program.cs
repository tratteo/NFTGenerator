// Copyright Matteo Beltrame

using HandierCli;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NFTGenerator.Services;
using System;
using System.IO;

Console.Title = "NFT Generator";

if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == null) Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");

var host = Host.CreateDefaultBuilder()
    .ConfigureAppConfiguration((config) =>
        config.SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", false, true)
        .AddEnvironmentVariables())
    .ConfigureServices((context, services) =>
    {
        services.AddTransient<IGenerator, Generator>();
        services.AddSingleton<IFilesystem, Filesystem>();
        services.AddSingleton<ICoreRunner, CommandLineService>();
    })
    .Build();

Logger.ConsoleInstance.LogInfo("----- NFT GENERATOR -----\n\n", ConsoleColor.DarkCyan);
var loggerFactory = host.Services.GetService<ILoggerFactory>();
var conf = host.Services.GetService<IConfiguration>();
if (loggerFactory != null)
{
    var logger = loggerFactory.CreateLogger("Bootstrap");
    logger.LogInformation("Running with env {env}", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));
}

var core = host.Services.GetService<ICoreRunner>();
if (core != null)
{
    await core.Run();
}