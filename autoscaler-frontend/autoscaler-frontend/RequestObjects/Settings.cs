public class Settings {
    public int? ScaleUp {set; get;}
    public int? ScaleDown {set; get;}
    public int? ScalePeriod {set; get;}
    public Settings()
    {
        
    }

    public Settings(int? scaleUp, int? scaleDown,int?scalePeriod)
    {
        ScaleUp = scaleUp;
        ScaleDown = scaleDown;
        ScalePeriod = scalePeriod;
    }
}