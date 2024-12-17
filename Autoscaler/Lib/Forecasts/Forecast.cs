namespace Autoscaler.Lib.Forecasts;

public class Forecast
{
    public readonly DateTime Timestamp;
    public readonly double Value;

    public Forecast()
    {
        
    }
    public Forecast(DateTime timestamp, double value)
    {
        Timestamp = timestamp;
        Value = value;
    }
}