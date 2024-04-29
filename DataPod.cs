using System;

namespace HeidGauge
{
    public class DataPod : IComparable<DataPod>
    {
        public double Target { get; }
        public double MeasurementValue { get; set; }
        public double Temperature { get; set; }
        public double Humidity { get; set; }
        public double MeasurementError { get; set; }
        public DateTime TimeStamp { get; private set; }
        public TimeSpan TimeSinceStart { get; set; }
        private bool Valid { get; set; }

        public DataPod(double target)
        {
            Target = target;
            TimeStamp = DateTime.UtcNow;
            InValidate();
        }

        public bool IsValid() => Valid;

        public void InValidate()
        {
            Valid = false;
            MeasurementValue = double.NaN;
            Temperature = double.NaN;
            Humidity = double.NaN;
        }

        public void SetTimeStamp()
        {
            TimeStamp = DateTime.UtcNow;
            Valid = true;

        }

        public string ToCsvHeader() => "Target (mm), Error (mm), Transducer (mm), Air temperature (°C), Humidity (%), Time stamp";

        public string ToCsvString() => $"{MaskNaN(Target)},{MaskNaN(MeasurementError)},{MaskNaN(MeasurementValue)},{MaskNaN(Temperature)},{MaskNaN(Humidity)},{TimeStamp.ToString("yyyy-MM-ddTHH:mm:ssZ")}";

        public string ToTerseString() => $"{Target,6:F3} mm {MeasurementValue,9:F5} mm {Temperature,6:F1} °C {Humidity,6:F1} %   {TimeStamp.ToString("yyyy-MM-ddTHH:mm:ssZ")}";

        public string ToVerbatimString() => $"{Target,8:F4} mm {MeasurementError,10:F5} mm {MeasurementValue,10:F5} mm {Temperature,6:F1} °C {Humidity,6:F1} %   {TimeSinceStart.TotalSeconds:F0} s";

        private string MaskNaN(double value) => double.IsNaN(value) ? " " : value.ToString();

        public override string ToString() => ToVerbatimString();

        public int CompareTo(DataPod other) => TimeStamp.CompareTo(other.TimeStamp);
    }
}
