using System.ComponentModel;

namespace ContainerRfBot.Bot.Helper;

public static class EnumExtensions
{
    public static string GetDescription<T>(this T enumValue) where T : Enum
    {
        var field = enumValue.GetType().GetField(enumValue.ToString());
        var attribute = field?.GetCustomAttributes(typeof(DescriptionAttribute), false)
            .FirstOrDefault() as DescriptionAttribute;
        
        return attribute?.Description ?? enumValue.ToString();
    }
}