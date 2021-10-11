// Copyright (c) Matteo Beltrame
//
// NFTGenerator -> Dependencies.cs
//
// All Rights Reserved

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace NFTGenerator
{
    internal static class Dependencies
    {
        public enum ProgramVersion { x86, x64 }

        private const string IMAGEMAGICK_URL = "https://download.imagemagick.org/ImageMagick/download/binaries/ImageMagick-7.1.0-8-Q16-HDRI-x64-dll.exe";
        private static readonly string DEPENDENCIES_TEMP_LOCATION = "C:/Users/" + Environment.GetEnvironmentVariable("USERNAME") + "/";

        public static void Resolve(Logger logger)
        {
            logger.LogInfo("Checking for dependencies...");
            ResolveDependency("ImageMagick", IMAGEMAGICK_URL, logger);
            logger.LogInfo("\n");
        }

        private static bool IsSoftwareInstalled(string applicationName, ProgramVersion? programVersion)
        {
            string[] registryKey = new[]
            {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
            };

            return registryKey.Any(key => CheckApplication(key, applicationName, programVersion));
        }

        private static void ResolveDependency(string name, string url, Logger logger)
        {
            if (!IsSoftwareInstalled(name, null))
            {
                logger.LogInfo("Missing dependency: " + name + " not installed", ConsoleColor.Red);
                logger.LogInfo("Downloading\n");
                string depInstallerPath = DEPENDENCIES_TEMP_LOCATION + name + "_dep.exe";
                using Process downloadProcess = new Process()
                {
                    EnableRaisingEvents = false
                };
                ProcessStartInfo startInfo = new()
                {
                    FileName = "cmd.exe",
                    Arguments = "/C curl " + url + " --output " + depInstallerPath
                };
                downloadProcess.StartInfo = startInfo;
                downloadProcess.Start();
                downloadProcess.WaitForExit();
                logger.LogInfo("\n" + name + " successfully downloaded, starting the installation process");
                Process proc = Process.Start(depInstallerPath);
                proc.EnableRaisingEvents = true;
                proc.Exited += (object sender, EventArgs e) =>
                {
                    if (proc.ExitCode != 0)
                    {
                        logger.LogError("Installation process failed, restart the application\nPress a key to exit");
                        File.Delete(depInstallerPath);
                        Console.ReadKey();
                        Environment.Exit(1);
                    }
                    else
                    {
                        logger.LogInfo(name + " successfully installed", ConsoleColor.Green);
                        File.Delete(depInstallerPath);
                    }
                };
                proc.WaitForExit();
            }
            else
            {
                logger.LogInfo(name + " dependency resolved", ConsoleColor.Green);
            }
        }

        private static IEnumerable<string> GetRegisterSubkeys(RegistryKey registryKey)
        {
            return registryKey.GetSubKeyNames()
                    .Select(registryKey.OpenSubKey)
                    .Select(subkey => subkey.GetValue("DisplayName") as string);
        }

        private static bool CheckApplication(string registryKey, string applicationName, ProgramVersion? programVersion)
        {
            RegistryKey key = Registry.LocalMachine.OpenSubKey(registryKey);
            if (key != null)
            {
                if (GetRegisterSubkeys(key).Any(displayName => displayName != null && displayName.Contains(applicationName) && displayName.Contains(programVersion.ToString())))
                {
                    return true;
                }

                key.Close();
            }

            return false;
        }
    }
}