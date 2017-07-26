﻿using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using TSQLLINT_CONSOLE.ConfigHandler.Interfaces;
using TSQLLINT_LIB.Config.Interfaces;
using TSQLLINT_LIB.Parser.Interfaces;

namespace TSQLLINT_CONSOLE.ConfigHandler
{
    public class CommandLineOptionHandler
    {
        public bool PerformLinting = true;

        private CommandLineOptions CommandLineOptions;
        private IConfigFileFinder ConfigFileFinder;
        private IConfigFileGenerator ConfigFileGenerator;
        private IBaseReporter Reporter;

        public CommandLineOptionHandler(CommandLineOptions commandLineOptions, IConfigFileFinder configFileFinder, IConfigFileGenerator configFileGenerator, IBaseReporter reporter)
        {
            CommandLineOptions = commandLineOptions;
            ConfigFileFinder = configFileFinder;
            ConfigFileGenerator = configFileGenerator;
            Reporter = reporter;
        }

        public void HandleCommandLineOptions()
        {
            if (CommandLineOptions.Args.Length == 0)
            {
                Reporter.Report(string.Format(CommandLineOptions.GetUsage()));
                PerformLinting = false;
            }

            CheckOptionsForNonLintingActions(CommandLineOptions);
            var configFileExists = ConfigFileFinder.FindFile(CommandLineOptions.ConfigFile);

            if (CommandLineOptions.Init)
            {
                var usersDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                var defaultConfigFile = Path.Combine(usersDirectory, @".tsqllintrc");
                var defaultConfigFileExists = ConfigFileFinder.FindFile(defaultConfigFile);

                if (!defaultConfigFileExists || CommandLineOptions.Force)
                {
                    ConfigFileGenerator.WriteConfigFile(defaultConfigFile);
                }
                else
                {
                    Reporter.Report(string.Format("Existing config file found at: {0} use the '--force' option to overwrite", defaultConfigFile));
                }
            }

            if (CommandLineOptions.Version)
            {
                ReportVersionInfo(Reporter);
            }

            if (CommandLineOptions.PrintConfig)
            {
                if (configFileExists)
                {
                    Reporter.Report(string.Format("Config file found at: {0}", CommandLineOptions.ConfigFile));
                }
                else
                {
                    Reporter.Report("Config file not found. You may generate it with the '--init' option");
                }
            }

            if (PerformLinting && !configFileExists)
            {
                Reporter.Report("Config file not found. You may generate it with the '--init' option");
                PerformLinting = false;
            }

            if (PerformLinting && string.IsNullOrWhiteSpace(CommandLineOptions.LintPath))
            {
                Reporter.Report("Linting path not provided");
                PerformLinting = false;
            }
        }

        private static void ReportVersionInfo(IBaseReporter reporter)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            var version = fvi.FileVersion;
            reporter.Report(string.Format("v{0}", version));
        }

        private void CheckOptionsForNonLintingActions(CommandLineOptions commandLineOptions)
        {
            var properties = typeof(CommandLineOptions).GetProperties();
            foreach (var prop in properties)
            {
                if (!PerformLinting)
                {
                    return;
                }

                var propertyValue = prop.GetValue(commandLineOptions);

                if (propertyValue == null)
                {
                    continue;
                }

                var propertyType = propertyValue.GetType();
                if (propertyType == typeof(bool))
                {
                    var value = (bool)propertyValue;

                    if (!value)
                    {
                        continue;
                    }
                }

                var attrs = prop.GetCustomAttributes(true);
                for (var index = 0; index < attrs.Length; index++)
                {
                    var attr = attrs[index];
                    var attrib = attr as TSQLLINTOption;

                    if (attrib != null && attrib.NonLintingCommand)
                    {
                        PerformLinting = false;
                        return;
                    }
                }
            }
        }
    }
}