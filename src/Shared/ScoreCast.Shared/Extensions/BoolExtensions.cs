namespace ScoreCast.Shared.Extensions;

public static class BoolExtensions
{
    public static string ConvertToTrueFalseString(this bool booleanValue)
    {
        return booleanValue.ToString();
    }

    public static string ConvertToYnString(this bool booleanValue)
    {
        return booleanValue ? "Y" : "N";
    }

    public static string ConvertToYnString(this bool? booleanValue)
    {
        return booleanValue == true ? "Y" : "N";
    }
}
