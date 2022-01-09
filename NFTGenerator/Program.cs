// Copyright Matteo Beltrame

using HandierCli;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NFTGenerator.Services;
using System;
using System.IO;

Console.Title = "NFT Generator";
Logger.ConsoleInstance.LogInfo("----- NFT GENERATOR -----\n\n", ConsoleColor.DarkCyan);

var host = Host.CreateDefaultBuilder()
    .ConfigureAppConfiguration((config) =>
        config.SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", true, true)
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", true, true)
        .AddEnvironmentVariables())
    .ConfigureServices((context, services) =>
    {
        services.AddTransient<IGenerator, Generator>();
        services.AddSingleton<IFilesystem, Filesystem>();
        services.AddSingleton<CommandLineService>();
    })
    .Build();

var core = host.Services.GetService<CommandLineService>();
if (core != null)
{
    await core.Run();
}