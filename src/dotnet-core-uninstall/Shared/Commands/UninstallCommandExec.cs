﻿using System;
using System.Collections.Generic;
using Microsoft.DotNet.Tools.Uninstall.Shared.Configs;
using Microsoft.DotNet.Tools.Uninstall.Shared.Exceptions;
using Microsoft.DotNet.Tools.Uninstall.Shared.BundleInfo;
using Microsoft.DotNet.Tools.Uninstall.Shared.Utils;
using Microsoft.DotNet.Tools.Uninstall.Windows;
using System.Reflection;
using Microsoft.DotNet.Tools.Uninstall.MacOs;

namespace Microsoft.DotNet.Tools.Uninstall.Shared.Commands
{
    internal static class UninstallCommandExec
    {
        private static readonly Lazy<string> _assemblyVersion =
            new Lazy<string>(() => {
                var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
                var assemblyVersionAttribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
                if (assemblyVersionAttribute == null)
                {
                    return assembly.GetName().Version.ToString();
                }
                else
                {
                    return assemblyVersionAttribute.InformationalVersion;
                }
            });

        public static void Execute()
        {
            HandleVersionOption();

            var filtered = GetFilteredBundles(GetAllBundles());

            if (CommandLineConfigs.CommandLineParseResult.RootCommandResult.OptionResult(CommandLineConfigs.DoItOption.Name) != null)
            {
                DoIt(filtered);
            }
            else
            {
                TryIt(filtered);
            }
        }

        private static IEnumerable<Bundle> GetAllBundles()
        {
            if (RuntimeInfo.RunningOnWindows)
            {
                return RegistryQuery.GetInstalledBundles();
            }
            else if (RuntimeInfo.RunningOnOSX)
            {
                return FileSystemExplorer.GetInstalledBundles();
            }
            else
            {
                throw new OperatingSystemNotSupportedException();
            }
        }

        private static IEnumerable<Bundle> GetFilteredBundles(IEnumerable<Bundle> bundles)
        {
            var option = CommandLineConfigs.CommandLineParseResult.RootCommandResult.GetUninstallMainOption();
            var typeSelection = CommandLineConfigs.CommandLineParseResult.RootCommandResult.GetTypeSelection();
            var archSelection = CommandLineConfigs.CommandLineParseResult.RootCommandResult.GetArchSelection();

            if (option == null)
            {
                if (CommandLineConfigs.CommandLineParseResult.RootCommandResult.Arguments.Count == 0)
                {
                    throw new RequiredArgMissingForUninstallCommandException();
                }

                return OptionFilterers.UninstallNoOptionFilterer.Filter(CommandLineConfigs.CommandLineParseResult.RootCommandResult.Arguments, bundles, typeSelection, archSelection);
            }
            else
            {
                return OptionFilterers.OptionFiltererDictionary[option].Filter(CommandLineConfigs.CommandLineParseResult, option, bundles, typeSelection, archSelection);
            }
        }

        private static void DoIt(IEnumerable<Bundle> bundles)
        {
            if (RuntimeInfo.RunningOnWindows)
            {
                Windows.UninstallCommandExec.ExecuteUninstallCommand(bundles);
            }
            else if (RuntimeInfo.RunningOnOSX)
            {
                throw new NotImplementedException();
            }
            else
            {
                throw new OperatingSystemNotSupportedException();
            }
        }

        private static void TryIt(IEnumerable<Bundle> bundles)
        {
            Console.WriteLine(LocalizableStrings.DryRunStartMessage);

            foreach (var bundle in bundles)
            {
                Console.WriteLine(string.Format(LocalizableStrings.DryRunBundleFormat, bundle.DisplayName));
            }

            Console.WriteLine(LocalizableStrings.DryRunEndMessage);
            Console.WriteLine();
            Console.WriteLine(string.Format(
                LocalizableStrings.DryRunHowToDoItMessageFormat,
                string.Join(" ", Environment.GetCommandLineArgs())));
        }

        private static void HandleVersionOption()
        {
            if (CommandLineConfigs.CommandLineParseResult.RootCommandResult.OptionResult(CommandLineConfigs.VersionOption.Name) != null)
            {
                Console.WriteLine(_assemblyVersion.Value);
                Environment.Exit(0);
            }
        }
    }
}