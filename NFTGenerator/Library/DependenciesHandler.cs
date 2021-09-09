using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NFTGenerator
{
    internal static class DependenciesHandler
    {
        public enum ProgramVersion { x86, x64 }

        public static void Resolve()
        {
            Logger.LogInfo("Checking for dependencies...");
            if (!Directory.Exists("dependencies"))
            {
                Logger.LogError("Something is really wrong, dependencies folder not found");
                Console.ReadKey();
                Environment.Exit(1);
            }
            string[] deps = Directory.GetFiles("dependencies/", "*.*");
            HandleDependency(deps, "ImageMagick");
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

        private static void HandleDependency(string[] deps, string name)
        {
            if (!IsSoftwareInstalled(name, null))
            {
                Logger.LogInfo("Missing dependency found: ImageMagick not installed");
                Regex reg = new Regex("ImageMagick");
                foreach (string dep in deps)
                {
                    if (reg.IsMatch(dep))
                    {
                        Logger.LogInfo("Installing ImageMagick");
                        using Process proc = Process.Start(dep);
                        proc.EnableRaisingEvents = true;
                        proc.Exited += (object sender, EventArgs e) =>
                        {
                            if (proc.ExitCode != 0)
                            {
                                Logger.LogError("Restart the application and install all the dependencies");
                                Console.ReadKey();
                                Environment.Exit(1);
                            }
                            else
                            {
                                Logger.LogInfo(dep + " successfully installed", ConsoleColor.Green);
                            }
                        };
                        proc.WaitForExit();
                    }
                }
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