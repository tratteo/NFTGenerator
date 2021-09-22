using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace NFTGenerator
{
    internal static class Dependencies
    {
        public enum ProgramVersion { x86, x64 }

        private const string IMAGEMAGICK_URL = "https://download.imagemagick.org/ImageMagick/download/binaries/ImageMagick-7.1.0-8-Q16-HDRI-x64-dll.exe";

        public static void Resolve()
        {
            Logger.LogInfo("Checking for dependencies...");
            ResolveDependency("ImageMagick", IMAGEMAGICK_URL);
            Logger.LogInfo();
        }

        public static bool IsSoftwareInstalled(string applicationName, ProgramVersion? programVersion)
        {
            string[] registryKey = new[]
            {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
            };

            return registryKey.Any(key => CheckApplication(key, applicationName, programVersion));
        }

        private static void ResolveDependency(string name, string url)
        {
            if (!IsSoftwareInstalled(name, null))
            {
                Logger.LogInfo("Missing dependency: " + name + " not installed", ConsoleColor.Red);
                Logger.LogInfo("Downloading\n");
                string installerName = name + "_dep.exe";
                using Process downloadProcess = new();
                ProcessStartInfo startInfo = new()
                {
                    FileName = "cmd.exe",
                    Arguments = "/C curl " + url + " --output " + installerName
                };
                downloadProcess.StartInfo = startInfo;
                downloadProcess.EnableRaisingEvents = true;
                downloadProcess.Start();
                downloadProcess.WaitForExit();
                Logger.LogInfo("\n" + name + " successfully downloaded, starting the installation process");
                using Process proc = Process.Start(installerName);
                proc.EnableRaisingEvents = true;
                proc.Exited += (object sender, EventArgs e) =>
                {
                    if (proc.ExitCode != 0)
                    {
                        Logger.LogError("Installation process failed, restart the application\nPress a key to exit");
                        File.Delete(installerName);

                        Console.ReadKey();
                        Environment.Exit(1);
                    }
                    else
                    {
                        Logger.LogInfo(name + " successfully installed", ConsoleColor.Green);
                        File.Delete(installerName);
                    }
                };
                proc.WaitForExit();
            }
            else
            {
                Logger.LogInfo(name + " dependency resolved", ConsoleColor.Green);
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