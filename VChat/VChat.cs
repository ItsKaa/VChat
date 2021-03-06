using BepInEx;
using HarmonyLib;

namespace VChat
{
    [BepInPlugin(GUID, Name, Version)]
    [HarmonyPatch]
    public class VChatPlugin : BaseUnityPlugin
    {
        public const string GUID = "org.itskaa.vchat";
        public const string Name = "VChat";
        public const string Version = "0.1.0";
        public const string Repository = "https://github.com/ItsKaa/VChat";

        public void Awake()
        {
            var harmony = new Harmony(GUID);
            harmony.PatchAll();
        }
    }
}
