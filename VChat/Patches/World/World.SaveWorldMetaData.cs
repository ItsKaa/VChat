using HarmonyLib;
using VChat.Services;

namespace VChat.Patches
{
    [HarmonyPatch(typeof(World), nameof(World.SaveWorldMetaData))]
    public static class WorldPatchSaveWorldMetaData
    {
        public static void Postfix(ref Player __instance)
        {
            PluginDataManager.Save();
        }
    }
}
