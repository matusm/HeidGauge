using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;

namespace HeidGauge
{
    class Options
    {
        [Option('p', "port", Default = " ", HelpText = "Serial port name for Heidenhain ND280.")]
        public string PortND280 { get; set; }

        [Option("thermo", Default = " ", HelpText = "Serial port name for thermohygrometer.")]
        public string PortThermo { get; set; }

        [Option("logfile", Default = "HeidGauge.log", HelpText = "Log file path.")]
        public string LogFileName { get; set; }

        [Option("comment", Default = "", HelpText = "User supplied comment string.")]
        public string UserComment { get; set; }

        [Option("prefix", Default = "HeidGauge", HelpText = "Prefix for output csv files.")]
        public string FilePrefix { get; set; }

        [Value(0, MetaName = "InputPath", Required = true, HelpText = "Target file-name including path")]
        public string InputPath { get; set; }

        public static void DisplayHelp<T>(ParserResult<T> result, IEnumerable<Error> errs)
        {
            string appName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
            HelpText helpText = HelpText.AutoBuild(result, h =>
            {
                h.AutoVersion = false;
                h.AdditionalNewLineAfterOption = false;
                h.AddPreOptionsLine("\nProgram ...");
                h.AddPreOptionsLine("");
                h.AddPreOptionsLine($"Usage: {appName} InputPath [OutPath] [options]");
                return HelpText.DefaultParsingErrorsHandler(result, h);
            }, e => e);
            Console.WriteLine(helpText);
        }

    }
}
