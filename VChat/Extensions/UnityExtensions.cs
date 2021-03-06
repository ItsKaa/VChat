using UnityEngine;

namespace VChat.Extensions
{
    public static class UnityExtensions
    {
        public static Color? GetColorFromString(this string nameOrHtmlString)
        {
            if (ColorUtility.TryParseHtmlString(nameOrHtmlString, out Color color))
            {
                return color;
            }
            return null;
        }
    }
}
