using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace VChat.Extensions
{
    public static class UnityExtensions
    {
        private static readonly Regex _stripUnityRichFormattingRegex = new(@"<(/)?(i|b|size|color|material|quad)=?.*?>", RegexOptions.IgnoreCase);
        private static readonly Regex _stripWhitespacesRegex = new("\\s*");

        public static Color? ToColor(this string nameOrHtmlString)
        {
            if (ColorUtility.TryParseHtmlString(nameOrHtmlString, out Color color))
            {
                return color;
            }
            return null;
        }

        public static string ToHtmlString(this Color color, bool includeAlpha = true, bool includeAlphaWhenNeeded = true)
        {
            return "#" + ((includeAlpha || (includeAlphaWhenNeeded && color.a < 1.0f)) ? ColorUtility.ToHtmlStringRGBA(color) : ColorUtility.ToHtmlStringRGB(color));
        }

        /// <summary>
        /// Changes the click-through value for a graphic.
        /// This will not affect any child components the graphic might have.
        /// </summary>
        public static void ChangeClickThrough(this Graphic graphic, bool enableClickThrough = true)
        {
            if (graphic != null)
            {
                graphic.raycastTarget = enableClickThrough;
            }
        }

        /// <summary>
        /// Changes the click-through value of every child component.
        /// </summary>
        public static void ChangeClickThroughInChildren(this Component parentComponent, bool enableClickThrough = true)
        {
            var chatWindowChildComponents = parentComponent.GetComponentsInChildren<Graphic>();
            foreach (var component in chatWindowChildComponents)
            {
                component.ChangeClickThrough(enableClickThrough);
            }
        }

        /// <summary>
        /// Returns a string without the known Unity rich text formatting.
        /// </summary>
        public static string StripRichTextFormatting(this string source)
        {
            return _stripUnityRichFormattingRegex.Replace(source, "");

        }

        /// <summary>
        /// Returns a string without whitespaces
        /// </summary>
        public static string StripWhitespaces(this string source)
        {
            return _stripWhitespacesRegex.Replace(source, "");

        }
    }
}
