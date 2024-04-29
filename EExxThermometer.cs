using System;
using Bev.Instruments.EplusE.EExx;

namespace HeidGauge
{
    public class EExxThermometer : IThermoHygrometer
    {
        private const double refreshPeriod = 1.5; // in seconds

        public EExxThermometer(string port)
        {
            thermometer = new EExx(port);
            Query();
        }

        public string InstrumentID => thermometer.InstrumentID;
        public double GetTemperature()
        {
            Refresh();
            return values.Temperature;
        }
        public double GetHumidity()
        {
            Refresh();
            return values.Humidity;
        }

        private void Refresh()
        {
            double timeElapsedSeconds = (DateTime.UtcNow - values.TimeStamp).TotalSeconds;
            if (timeElapsedSeconds > refreshPeriod) 
                Query();
        }

        private void Query() => values = thermometer.GetValues();

        private readonly EExx thermometer;
        private MeasurementValues values;
    }
}
