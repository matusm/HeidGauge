using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using At.Matus.StatisticPod;
using Bev.Instruments.Heidenhain;
using System.Reflection;
using System.Threading;
using Bev.UI;

namespace HeidGauge
{
    class Program
    {
        
        private static StreamWriter logFile;
        private static Options options = new Options(); // this must be set in Run()
        private static DataPod[] dataBase;
        private static StatisticPod temp = new StatisticPod();
        private static StatisticPod humi = new StatisticPod();
        private static DateTime startTime;

        static void Main(string[] args)
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            Parser parser = new Parser(with => with.HelpWriter = null);
            ParserResult<Options> parserResult = parser.ParseArguments<Options>(args);
            parserResult
                .WithParsed<Options>(options => Run(options))
                .WithNotParsed(errs => Options.DisplayHelp(parserResult, errs));
        }

        // Run() is the new Main()
        private static void Run(Options ops)
        {
            options = ops;

            #region Instantiate hardware

            startTime = DateTime.UtcNow;
            IHeidenhain lengthGauge; 
            IThermoHygrometer thermo;
            using (new InfoOperation("Setting up hardware"))
            {
                if (string.IsNullOrWhiteSpace(options.PortND280))
                    lengthGauge = new DummyND();
                else
                    lengthGauge = new ND280(options.PortND280);

                if (string.IsNullOrWhiteSpace(options.PortThermo))
                    thermo = new DummyThermometer();
                else
                {
                    thermo = new EExxThermometer(options.PortThermo);
                    _ = thermo.InstrumentID;
                    // EE07 needs some measurements to stabilize humidity measurement values
                    for (int i = 0; i < 5; i++)
                    {
                        _ = thermo.GetHumidity();
                        Thread.Sleep(2000);
                    }
                }
            }
                
            #endregion

            #region File stuff

            logFile = new StreamWriter(options.LogFileName, true);
            LoadTargets(options.InputPath);
            string csvFilename = $"{options.FilePrefix}_{Path.GetFileNameWithoutExtension(options.InputPath)}_{startTime:yyyyMMddHHmm}.csv";

            #endregion

            #region Diagnostic output
            DisplayOnly("");
            LogOnly(fatSeparator);
            LogAndDisplay($"Application:       {Assembly.GetExecutingAssembly().GetName().Name} {Assembly.GetExecutingAssembly().GetName().Version}");
            LogAndDisplay($"StartTimeUTC:      {startTime:dd-MM-yyyy HH:mm}");
            LogAndDisplay($"GaugeID:           {lengthGauge.InstrumentID}");
            LogAndDisplay($"ThermoHygroID:     {thermo.InstrumentID}");
            LogAndDisplay($"Target file:       {Path.GetFileName(options.InputPath)}");
            LogAndDisplay($"Number of targets: {dataBase.Length}");
            LogAndDisplay($"Result file:       {csvFilename}");
            LogAndDisplay($"Comment:           {options.UserComment}");
            LogOnly(thinSeparator);
            DisplayOnly("");
            #endregion

            int measurementIndex = 0;
            bool shallLoop = true;
            while (shallLoop && (measurementIndex < dataBase.Length))
            {
                DisplayOnly("press 'space' or 'enter' to record a measurement, 'd' to delete previous, 'q' to quit");
                while (Console.KeyAvailable == false)
                {
                    Thread.Sleep(100);
                    _ = thermo.GetHumidity();
                    Console.Write($"\r   Target {measurementIndex+1} of {dataBase.Length} ({dataBase[measurementIndex].Target,8:F3} mm):    {lengthGauge.GetValue(),9:F4} mm ");
                }
                DisplayOnly();
            
                ConsoleKeyInfo cki = Console.ReadKey(true);
                switch (cki.Key)
                {
                    case ConsoleKey.Q:
                        shallLoop = false;
                        LogOnly("Aborted!");
                        break;
                    case ConsoleKey.Spacebar:
                    case ConsoleKey.Enter:
                        dataBase[measurementIndex].SetTimeStamp();
                        dataBase[measurementIndex].MeasurementValue = lengthGauge.GetValue();
                        dataBase[measurementIndex].Temperature = thermo.GetTemperature();
                        dataBase[measurementIndex].Humidity = thermo.GetHumidity();
                        LogAndDisplay($"   {dataBase[measurementIndex].ToTerseString()}");
                        DisplayOnly();
                        measurementIndex++;
                        AudioUI.BeepHigh();
                        break;
                    case ConsoleKey.D:
                        if (measurementIndex == 0) 
                            break;
                        measurementIndex--;
                        dataBase[measurementIndex].InValidate();
                        LogOnly("Entry deleted!");
                        DisplayOnly();
                        AudioUI.BeepLow();
                        break;
                    default:
                        break;
                }
            }

            CalculateErrors(false);
            EvaluateEnvironmentalData();
            DisplayOnly();
            LogOnly(thinSeparator);
            foreach (var d in dataBase)
            {
                if(d.IsValid())
                    LogAndDisplay($"   {d.ToVerbatimString()}");
            }
            LogOnly(thinSeparator);
            LogOnly($"Duration:            {GetDuration().Hours} h {GetDuration().Minutes} min");
            LogOnly($"Average temperature: {temp.AverageValue:F2} °C  [{temp.MinimumValue:F2} - {temp.MaximumValue:F2}]");
            LogOnly($"Average humidity:    {humi.AverageValue:F1} %  [{humi.MinimumValue:F1} - {humi.MaximumValue:F1}]");
            LogOnly(fatSeparator);
            DisplayOnly();
            WriteResultsToCsv(csvFilename);
            DisplayOnly("done");
        }

        /***************************************************/
        private static void WriteResultsToCsv(string filename)
        {
            if (dataBase.Length == 0) return;
            using (StreamWriter sw = new StreamWriter(filename, false))
            {
                string csvHeader = dataBase[0].ToCsvHeader();
                sw.WriteLine(csvHeader);
                foreach (var d in dataBase)
                {
                    sw.WriteLine(d.ToCsvString());
                }
            }
        }
        /***************************************************/
        private static void LoadTargets(string inputPath)
        {
            List<DataPod> dataList = new List<DataPod>();
            using (StreamReader reader = new StreamReader(inputPath))
            {
                while (!reader.EndOfStream)
                {
                    string str = reader.ReadLine();
                    if (double.TryParse(str, out double target))
                        dataList.Add(new DataPod(target));
                }
            }
            dataBase = dataList.ToArray();
        }
        /***************************************************/
        private static void CalculateErrors(bool opoDirection)
        {
            if (dataBase == null) return;
            if (dataBase.Length < 1) return;
            int sign = 1;
            if (opoDirection) sign = -1;
            double origin = sign * dataBase[0].MeasurementValue;
            DateTime start = dataBase[0].TimeStamp;
            for (int i = 0; i < dataBase.Length; i++)
            {
                var data = dataBase[i];
                double error = data.Target - (sign*data.MeasurementValue - origin);
                TimeSpan duration = data.TimeStamp - start;
                dataBase[i].MeasurementError = error;
                dataBase[i].TimeSinceStart = duration;
            }
        }
        /***************************************************/
        private static void EvaluateEnvironmentalData()
        {
            foreach (var d in dataBase)
            {
                if (d.IsValid())
                {
                    temp.Update(d.Temperature);
                    humi.Update(d.Humidity);
                }
            }
        }
        /***************************************************/
        private static TimeSpan GetDuration() => dataBase.Last().TimeStamp - startTime;
        /***************************************************/
        static void LogAndDisplay(string line)
        {
            DisplayOnly(line);
            LogOnly(line);
        }
        static void LogAndDisplay() => LogAndDisplay("");
        /***************************************************/
        static void LogOnly(string line)
        {
            logFile.WriteLine(line);
            logFile.Flush();
        }
        static void LogOnly() => LogOnly("");
        /***************************************************/
        static void DisplayOnly(string line)
        {
            Console.WriteLine(line);
        }
        static void DisplayOnly() => DisplayOnly("");
        /***************************************************/

        private static readonly string fatSeparator = new string('=', 80);
        private static readonly string thinSeparator = new string('-', 80);

    }
}
