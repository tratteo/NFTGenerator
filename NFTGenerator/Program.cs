// Copyright Matteo Beltrame

using HandierCli;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using NFTGenerator.Services;
using NFTGenerator.Settings;
using System;

Console.Title = "NFT Generator";
Logger.ConsoleInstance.LogInfo("----- NFT GENERATOR -----\n\n", ConsoleColor.DarkCyan);

var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<GenerationSettings>(builder.Configuration.GetSection(GenerationSettings.Position));
builder.Services.AddTransient<IGenerator, Generator>();
builder.Services.AddSingleton<IFilesystem, Filesystem>();
builder.Services.AddSingleton<CommandLineService>();

var app = builder.Build();
var cmd = app.Services.GetRequiredService<CommandLineService>();
await cmd.Run();