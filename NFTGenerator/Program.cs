﻿// Copyright Matteo Beltrame

using HandierCli;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using NFTGenerator.Services;
using NFTGenerator.Settings;
using NFTGenerator.Statics;
using System;
using System.Threading;

Console.Title = "NFT Generator";
Logger.ConsoleInstance.LogInfo("----- NFT GENERATOR -----\n\n", ConsoleColor.DarkCyan);

var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<GenerationSettings>(builder.Configuration.GetSection(GenerationSettings.Position));
builder.Services.AddTransient<IGenerator, Generator>();
builder.Services.AddSingleton<IFilesystem, Filesystem>();

builder.Services.AddOfflineRunner<CommandLineService>();

var app = builder.Build();
await app.RunOfflineAsync(CancellationToken.None);