namespace HeidGauge
{
    public class DummyThermometer : IThermoHygrometer
    {
        public string InstrumentID => "Dummy ThermoHygrometer";
        public double GetTemperature() => 20;
        public double GetHumidity() => 50;
    }
}
