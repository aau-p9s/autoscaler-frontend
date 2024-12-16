class Forecast {
    public readonly DateTime Timestamp;
    public readonly int Value;

    public Forecast(DateTime timestamp, int value) {
        Timestamp = timestamp;
        Value = value;
    }
}