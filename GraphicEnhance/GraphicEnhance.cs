using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using UnityEngine;

namespace COM3D2.GraphicEnhance
{
    public static class PluginInfo
    {
        public const string PLUGIN_GUID = "COM3D2.GraphicEnhance";
        public const string PLUGIN_NAME = "GraphicEnhance";
        public const string PLUGIN_VERSION = "1.0.0";
    }

    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public sealed class GraphicEnhance : BaseUnityPlugin
    {
        public static GraphicEnhance Instance { get; private set; }
        public static Harmony harmony { get; private set; }
        internal static new ManualLogSource Logger => Instance?._Logger;
        private ManualLogSource _Logger => base.Logger;
        // Global
        internal static ConfigEntry<int> threadPoolSize;
        // SkinEnhance
        internal static ConfigEntry<SkinMethod> SkinMethodConfig;
        internal static ConfigEntry<KeyCode> SkinMethodSwitchHotkeyModifier;
        internal static ConfigEntry<KeyCode> SkinMethodSwitchHotkeyKey;
        // ShapekeyEnhance
        internal static ConfigEntry<bool> ShapekeyEnhanceEnable;
        internal static ConfigEntry<ShapekeyMethod> ShapekeyEnhanceMethod;
        internal static ConfigEntry<int> ShapekeyEnhanceFullSyncInterval;
        internal static ConfigEntry<bool> ShapekeyEnhanceBlendPosNormFix;
        // TextureCache
        internal static ConfigEntry<bool> TextureCacheEnable;
        internal static ConfigEntry<bool> TextureCacheAlwaysLoadCheck;
        // TextureExtend
        internal static ConfigEntry<bool> TextureExtendEnable;

        private void Awake()
        {
            Instance = this;
            GraphicPatch.Init();
            bool result = GraphicPatch.PatchInit();
            if (!result)
            {
                Logger.LogError("GraphicPatch initialization failed.");
                return;
            }
            InitConfig();
            // ThreadPool
            GraphicPatch.ThreadPoolInit(threadPoolSize.Value);
            // SkinEnhance
            harmony = Harmony.CreateAndPatchAll(typeof(SkinEnhance));
            // ShapekeyEnhance
            harmony.PatchAll(typeof(ShapekeyEnhance));
            // TextureCache
            harmony.PatchAll(typeof(TextureCache));
            TextureCache.SubInit(harmony);
            // TextureExtend
            harmony.PatchAll(typeof(TextureExtend));
        }

        private void OnDestroy()
        {
            harmony?.UnpatchSelf();
            GraphicPatch.ThreadPoolDeinit();
        }

        private void InitConfig()
        {
            // Global
            int maxPoolSize = GraphicPatch.GetLogicalProcessorCount();
            int defaultPoolSize = Math.Max(maxPoolSize - 1, 1);
            threadPoolSize = Config.Bind("Global", "ThreadPoolSize", defaultPoolSize, new ConfigDescription("Thread Pool Size (Need Restart)", new AcceptableValueRange<int>(1, maxPoolSize)));
            if (threadPoolSize.Value <= 0)
            {
                threadPoolSize.Value = defaultPoolSize;
            }
            if (threadPoolSize.Value > maxPoolSize)
            {
                threadPoolSize.Value = maxPoolSize;
            }
            // SkinEnhance
            SkinMethodConfig = Config.Bind("SkinEnhance", "SkinMethod", SkinMethod.LBS, "Skin Method");
            SkinMethodSwitchHotkeyModifier = Config.Bind("SkinEnhance", "SwitchHotkeyModifier", KeyCode.None, "Hotkey Modifier Key");
            SkinMethodSwitchHotkeyKey = Config.Bind("SkinEnhance", "SwitchHotkeyKey", KeyCode.None, "Hotkey Main Key");
            SkinMethodConfig.SettingChanged += (sender, args) => {
                GraphicPatch.SetSwitchDqsOn(SkinMethodConfig.Value == SkinMethod.DQS);
            };
            GraphicPatch.SetSwitchDqsOn(SkinMethodConfig.Value == SkinMethod.DQS);
            // ShapekeyEnhance
            ShapekeyEnhanceEnable = Config.Bind("ShapekeyEnhance", "_GlobalEnable", true, "Global Switch");
            ShapekeyEnhanceMethod = Config.Bind("ShapekeyEnhance", "ShapekeyMethod", ShapekeyMethod.Delta, "Shapekey Enhance Method");
            ShapekeyEnhanceFullSyncInterval = Config.Bind("ShapekeyEnhance", "DeltaModeFullSyncInterval", 120, "Delta Mode Full Sync Interval (s)");
            ShapekeyEnhanceBlendPosNormFix = Config.Bind("ShapekeyEnhance", "DeltaModeBlendPosNormFix", false, "Fix with some shapekey plugin");
            ShapekeyEnhanceEnable.SettingChanged += (sender, args) =>
            {
                ShapekeyEnhance.globalEnable = ShapekeyEnhanceEnable.Value;
            };
            ShapekeyEnhanceMethod.SettingChanged += (sender, args) => {
                ShapekeyEnhance.shapekeyMethod = ShapekeyEnhanceMethod.Value;
                if (ShapekeyEnhance.shapekeyMethod == ShapekeyMethod.Delta)
                    ShapekeyEnhance.lastSyncTime = (int)Time.realtimeSinceStartup;
            };
            ShapekeyEnhanceFullSyncInterval.SettingChanged += (sender, args) => {
                ShapekeyEnhance.deltaFullSyncInterval = ShapekeyEnhanceFullSyncInterval.Value;
            };
            ShapekeyEnhanceBlendPosNormFix.SettingChanged += (sender, args) => {
                ShapekeyEnhance.deltaBlendPosNormFix = ShapekeyEnhanceBlendPosNormFix.Value;
            };
            ShapekeyEnhance.globalEnable = ShapekeyEnhanceEnable.Value;
            ShapekeyEnhance.shapekeyMethod = ShapekeyEnhanceMethod.Value;
            if (ShapekeyEnhance.shapekeyMethod == ShapekeyMethod.Delta)
                ShapekeyEnhance.lastSyncTime = (int)Time.realtimeSinceStartup;
            ShapekeyEnhance.deltaFullSyncInterval = ShapekeyEnhanceFullSyncInterval.Value;
            ShapekeyEnhance.deltaBlendPosNormFix = ShapekeyEnhanceBlendPosNormFix.Value;
            // TextureCache
            TextureCacheEnable = Config.Bind("TextureCache", "_GlobalEnable", true, "Global Switch");
            TextureCacheAlwaysLoadCheck = Config.Bind("TextureCache", "AlwaysLoadCheck", false, "Always Load Check");
            TextureCacheEnable.SettingChanged += (sender, args) => {
                TextureCache.globalEnable = TextureCacheEnable.Value;
            };
            TextureCacheAlwaysLoadCheck.SettingChanged += (sender, args) => {
                TextureCache.alwaysLoadCheck = TextureCacheAlwaysLoadCheck.Value;
            };
            TextureCache.globalEnable = TextureCacheEnable.Value;
            TextureCache.alwaysLoadCheck = TextureCacheAlwaysLoadCheck.Value;
            // TextureExtend
            TextureExtendEnable = Config.Bind("TextureExtend", "_GlobalEnable", true, "Global Switch");
            TextureExtendEnable.SettingChanged += (sender, args) => {
                TextureExtend.globalEnable = TextureExtendEnable.Value;
            };
            TextureExtend.globalEnable = TextureExtendEnable.Value;
        }

        private void Update()
        {
#pragma warning disable ULib004
            // SkinEnhance
            if (SkinMethodSwitchHotkeyKey.Value != KeyCode.None)
            {
                bool modifierPressed = SkinMethodSwitchHotkeyModifier.Value == KeyCode.None || Input.GetKey(SkinMethodSwitchHotkeyModifier.Value);
                if (modifierPressed && Input.GetKeyDown(SkinMethodSwitchHotkeyKey.Value))
                {
                    SkinMethodConfig.Value = SkinMethodConfig.Value == SkinMethod.LBS ? SkinMethod.DQS : SkinMethod.LBS;
                }
            }
            // ShapekeyEnhance
            if (ShapekeyEnhance.shapekeyMethod == ShapekeyMethod.Delta)
            {
                int currentTime = (int)Time.realtimeSinceStartup;
                if (ShapekeyEnhance.deltaFullSyncInterval > 0 && currentTime - ShapekeyEnhance.lastSyncTime >= ShapekeyEnhance.deltaFullSyncInterval)
                {
                    ShapekeyEnhance.deltaFullSyncFlag = !ShapekeyEnhance.deltaFullSyncFlag;
                    ShapekeyEnhance.lastSyncTime = currentTime;
                }
            }
#pragma warning restore ULib004
        }
    }
}
