using UnityEngine;
using UnityEngine.UI;

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
    }
}
