using HarmonyLib;

namespace VChat.Patches
{
    [HarmonyPatch(typeof(Game), nameof(Game.Update))]
    public static class GamePatchUpdate
    {
        private static void Postfix(ref Game __instance)
        {
            VChatPlugin.Settings.Tick();
        }
    }
}
