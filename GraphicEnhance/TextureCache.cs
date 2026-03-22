using COM3D2.GraphicEnhance.Util;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;


namespace COM3D2.GraphicEnhance
{
    internal class TextureCache
    {
        internal static bool globalEnable = false;
        internal static bool alwaysLoadCheck = false;
        internal static int texTempFileSize;
        internal static Texture2DCache texCache = new Texture2DCache();

        internal static void SubInit(Harmony harmony)
        {
            new TryPatchMaidLoader(harmony);
#if TEXTURECACHE_DEBUG
            TextureCacheDebug.Init();
#endif
        }

        public static Texture2D GetTexture2D(string f_strFileName, AFileSystemBase f_fileSystem = null)
        {
            if (globalEnable)
            {
                if (texCache.TryGet(f_strFileName.ToLower(), out UObjectHandle<Texture2D> handle, f_fileSystem != null ? f_fileSystem : GameUty.FileSystem))
                {
                    return handle.obj;
                }
            }
            return null;
        }

        public static Texture2D CacheTexture2D(Texture2D tex, string f_strFileName)
        {
            if (globalEnable)
            {
                texCache.Add(f_strFileName.ToLower(), tex, ImportCM.m_texTempFile, texTempFileSize);
            }
            return tex;
        }
        
        public static void SetTexTempFileSize(int size)
        {
            texTempFileSize = size;
        }

        public static void SetAllDirty()
        {
            texCache.SetAllDirty();
        }
        
        // `ImportCM.CreateTexture`
        /*
        public static Texture2D CreateTexture(string f_strFileName)
        {
+           Texture2D tex = TextureCache.GetTexture2D(f_strFileName, null);
+           if (tex != null)
+           {
+               return tex;
+           }
            tex = ImportCM.LoadTexture(GameUty.FileSystem, f_strFileName, true).CreateTexture2D();
+           tex = TextureCache.CacheTexture2D(tex, f_strFileName);
            return tex;
        }
        */
        [HarmonyPatch(typeof(ImportCM), nameof(ImportCM.CreateTexture), new[] { typeof(string) })]
        [HarmonyTranspiler]
        internal static IEnumerable<CodeInstruction> CreateTexture1Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            LocalBuilder tex = generator.DeclareLocal(typeof(Texture2D));
            Label label;
            CodeMatcher codeMatcher = new CodeMatcher(instructions, generator);
            codeMatcher.Start()
                .CreateLabel(out label)
                .InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0))
                .InsertAndAdvance(new CodeInstruction(OpCodes.Ldnull))
                .InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TextureCache), nameof(GetTexture2D))))
                .InsertAndAdvance(new CodeInstruction(OpCodes.Stloc_S, tex))
                .InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_S, tex))
                .InsertAndAdvance(new CodeInstruction(OpCodes.Ldnull))
                .InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(UnityEngine.Object), "op_Equality")))
                .InsertAndAdvance(new CodeInstruction(OpCodes.Brtrue_S, label))
                .InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_S, tex))
                .InsertAndAdvance(new CodeInstruction(OpCodes.Ret))
                .End()
                .MatchBack(false, new[] { new CodeMatch(OpCodes.Ret) })
                .InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0))
                .InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TextureCache), nameof(CacheTexture2D))));
            return codeMatcher.InstructionEnumeration();
        }

        // `ImportCM.CreateTexture`
        /*
        public static Texture2D CreateTexture(AFileSystemBase f_fileSystem, string f_strFileName)
        {
+           Texture2D tex = TextureCache.GetTexture2D(f_strFileName, f_fileSystem);
+           if (tex != null)
+           {
+               return tex;
+           }
            tex = ImportCM.LoadTexture(f_fileSystem, f_strFileName, true).CreateTexture2D();
+           tex = TextureCache.AddTexture2D(tex, f_strFileName);
            return tex;
        }
        */
        [HarmonyPatch(typeof(ImportCM), nameof(ImportCM.CreateTexture), new[] { typeof(AFileSystemBase), typeof(string) })]
        [HarmonyTranspiler]
        internal static IEnumerable<CodeInstruction> CreateTexture2Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            LocalBuilder tex = generator.DeclareLocal(typeof(Texture2D));
            Label label;
            CodeMatcher codeMatcher = new CodeMatcher(instructions, generator);
            codeMatcher.Start()
                .CreateLabel(out label)
                .InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_1))
                .InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0))
                .InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TextureCache), nameof(GetTexture2D))))
                .InsertAndAdvance(new CodeInstruction(OpCodes.Stloc_S, tex))
                .InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_S, tex))
                .InsertAndAdvance(new CodeInstruction(OpCodes.Ldnull))
                .InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(UnityEngine.Object), "op_Equality")))
                .InsertAndAdvance(new CodeInstruction(OpCodes.Brtrue_S, label))
                .InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_S, tex))
                .InsertAndAdvance(new CodeInstruction(OpCodes.Ret))
                .End()
                .MatchBack(false, new[] { new CodeMatch(OpCodes.Ret) })
                .InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_1))
                .InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TextureCache), nameof(CacheTexture2D))));
            return codeMatcher.InstructionEnumeration();
        }

        // `TBody.ChangeTex`
        /*
        + TextureCache.SetTexTempFileSize(array.Length)
        textureResource = new TextureResource(2, 2, 5, null, array);
        ...
+       Texture2D texture2D = TextureCache.GetTexture2D(filename, null);
+       if (texture2D == null)
+       {
            texture2D = textureResource.CreateTexture2D();
            texture2D.name = filename;
+           TextureCache.CacheTexture2D(texture2D, filename);
+       }
        */
        [HarmonyPatch(typeof(TBody), nameof(TBody.ChangeTex))]
        [HarmonyTranspiler]
        internal static IEnumerable<CodeInstruction> ChangeTexTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var locals = Traverse.Create(Traverse.Create(generator).Field("Target").GetValue<MonoMod.Utils.Cil.CecilILGenerator>()).Field("_Variables").GetValue<Dictionary<LocalBuilder, Mono.Cecil.Cil.VariableDefinition>>().Keys.ToArray();
            object texture2D = null;
            foreach (LocalBuilder local in locals)
            {
                if (local.LocalType == typeof(Texture2D))
                {
                    texture2D = local;
                    break;
                }
            }
            if (texture2D == null)
            {
                throw new Exception("Local Variables not Found");
            }
            Label label;
            CodeMatcher codeMatcher = new CodeMatcher(instructions, generator);
            codeMatcher.Start()
               .MatchForward(false, new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(string), nameof(string.ToLower))));
            var loadFilenameField = codeMatcher.InstructionsWithOffsets(-2, -1);
            codeMatcher.Start()
                .MatchForward(false, new CodeMatch(OpCodes.Newobj, AccessTools.Constructor(typeof(TextureResource), new[] {typeof(int), typeof(int), typeof(TextureFormat), typeof(Rect[]), typeof(byte[]) })))
                .InsertAndAdvance(new CodeInstruction(OpCodes.Dup))
                .InsertAndAdvance(new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(byte[]), "Length")))
                .InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TextureCache), nameof(SetTexTempFileSize))))
                .MatchForward(false, new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(TextureResource), nameof(TextureResource.CreateTexture2D))))
                .MatchForward(false, new[] { new CodeMatch(OpCodes.Callvirt, AccessTools.PropertySetter(typeof(UnityEngine.Object), nameof(UnityEngine.Object.name))) })
                .Advance(1)
                .InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_S, texture2D))
                .InsertAndAdvance(loadFilenameField)
                .InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TextureCache), nameof(CacheTexture2D))))
                .InsertAndAdvance(new CodeInstruction(OpCodes.Pop))
                .CreateLabel(out label)
                .MatchBack(false, new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(TextureResource), nameof(TextureResource.CreateTexture2D))))
                .Advance(-1)
                .InsertAndAdvance(loadFilenameField)
                .InsertAndAdvance(new CodeInstruction(OpCodes.Ldnull))
                .InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TextureCache), nameof(GetTexture2D))))
                .InsertAndAdvance(new CodeInstruction(OpCodes.Dup))
                .InsertAndAdvance(new CodeInstruction(OpCodes.Stloc_S, texture2D))
                .InsertAndAdvance(new CodeInstruction(OpCodes.Ldnull))
                .InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(UnityEngine.Object), "op_Equality")))
                .InsertAndAdvance(new CodeInstruction(OpCodes.Brfalse_S, label));
            return codeMatcher.InstructionEnumeration();
        }

        [HarmonyPatch(typeof(ImportCM), nameof(ImportCM.LoadTextureFile))]
        [HarmonyPostfix]
        internal static void LoadTextureFilePostfix(ref TextureResource __result, AFileBase file)
        {
            if (__result != null)
            {
                texTempFileSize = file.GetSize();
            }
        }

        [HarmonyPatch(typeof(TextureResource), nameof(TextureResource.CreateTexture2D))]
        [HarmonyPostfix]
        internal static void CreateTexture2DPostfix(ref Texture2D __result)
        {
            if (__result != null)
            {
                __result.Apply(false, true);
            }
        }

        [HarmonyPatch(typeof(UnityEngine.Object), nameof(UnityEngine.Object.Destroy), new[] { typeof(UnityEngine.Object) })]
        [HarmonyPrefix]
        internal static bool ObjectDestroyPrefix(UnityEngine.Object obj)
        {
            if (obj is Texture2D tex)
            {
                return texCache.Release(tex, false);
            }
            return true;
        }

        [HarmonyPatch(typeof(UnityEngine.Object), nameof(UnityEngine.Object.DestroyImmediate), new[] { typeof(UnityEngine.Object) })]
        [HarmonyPrefix]
        internal static bool ObjectDestroyImmediatePrefix(UnityEngine.Object obj)
        {
            if (obj is Texture2D tex)
            {
                return texCache.Release(tex, true);
            }
            return true;
        }

        [HarmonyPatch(typeof(Texture2D), nameof(Texture2D.GetPixel))]
        [HarmonyPatch(typeof(Texture2D), nameof(Texture2D.GetPixelBilinear))]
        [HarmonyPatch(typeof(Texture2D), nameof(Texture2D.GetPixels), new[] { typeof(int), typeof(int), typeof(int), typeof(int), typeof(int) })]
        [HarmonyPatch(typeof(Texture2D), nameof(Texture2D.GetPixels32), new[] { typeof(int) })]
        [HarmonyPatch(typeof(Texture2D), nameof(Texture2D.SetPixel))]
        [HarmonyPatch(typeof(Texture2D), nameof(Texture2D.SetPixels), new[] { typeof(int), typeof(int), typeof(int), typeof(int), typeof(Color[]), typeof(int) })]
        [HarmonyPatch(typeof(Texture2D), nameof(Texture2D.SetPixels32), new[] { typeof(Color32[]), typeof(int) })]
        [HarmonyPatch(typeof(Texture2D), nameof(Texture2D.SetPixels32), new[] { typeof(int), typeof(int), typeof(int), typeof(int), typeof(Color32[]), typeof(int) })]
        [HarmonyPatch(typeof(Texture2D), nameof(Texture2D.EncodeToPNG))]
        [HarmonyPatch(typeof(Texture2D), nameof(Texture2D.EncodeToJPG), new[] { typeof(int) })]
        //[HarmonyPatch(typeof(Texture2D), nameof(Texture2D.EncodeToEXR), new[] { typeof(EXRFlags) })]
        //[HarmonyPatch(typeof(Texture2D), nameof(Texture2D.LoadRawTextureData), new[] { typeof(byte[]) })]
        //[HarmonyPatch(typeof(Texture2D), nameof(Texture2D.LoadRawTextureData), new[] { typeof(byte[]), typeof(int) })]
        //[HarmonyPatch(typeof(Texture2D), nameof(Texture2D.LoadImage), new[] { typeof(byte[]), typeof(bool) })]
        //[HarmonyPatch(typeof(Texture2D), nameof(Texture2D.ReadPixels), new[] { typeof(Rect), typeof(int), typeof(int) })]
        //[HarmonyPatch(typeof(Texture2D), nameof(Texture2D.ReadPixels), new[] { typeof(Rect), typeof(int), typeof(int), typeof(bool) })]
        [HarmonyPrefix]
        internal static void CopytoMem(Texture2D __instance)
        {
            __instance.CopyToMem();
        }

        internal class UObjectHandle<T> where T : UnityEngine.Object
        {
            internal string objName;
            internal T obj;
            internal int refCount = 1;
            internal ulong hash = 0;
            internal int length = 0;
            internal bool dirty = false;

            public UObjectHandle(string name, T o, byte[] data, int len)
            {
                objName = name;
                obj = o;
                if (data != null)
                    hash = GraphicPatch.XXHash3_64(data, len);
                length = len;
                dirty = false;
            }

            public void AddRef()
            {
                refCount++;
            }

            public bool Release(bool immediate = false)
            {
                refCount--;
                if (refCount <= 0)
                {
                    DestroyInternal(immediate);
                    return true;
                }
                return false;
            }

            void DestroyInternal(bool immediate)
            {
                if (immediate)
                    UnityEngine.Object.DestroyImmediate(obj, false);
                else
                    UnityEngine.Object.Destroy(obj, 0f);
                obj = null;
            }
        }

        internal class Texture2DCache
        {
            internal Dictionary<string, UObjectHandle<Texture2D>> cache = new Dictionary<string, UObjectHandle<Texture2D>>();
            internal Dictionary<Texture2D, UObjectHandle<Texture2D>> reverseCache = new Dictionary<Texture2D, UObjectHandle<Texture2D>>();

            public UObjectHandle<Texture2D> Add(string key, Texture2D tex, byte[] data, int len)
            {
                var handle = new UObjectHandle<Texture2D>(key, tex, data, len);
                cache[key] = handle;
                reverseCache[tex] = handle;
                return handle;
            }

            internal bool Get(string key, out UObjectHandle<Texture2D> handle)
            {
                if (!cache.TryGetValue(key, out handle))
                    return false;
                if (handle.obj == null)
                    return false;
                return true;
            }

            public bool TryGet(string key, out UObjectHandle<Texture2D> handle, AFileSystemBase f_fileSystem = null)
            {
                if (!cache.TryGetValue(key, out handle))
                    return false;
                if (handle.obj == null)
                    return false;
                handle.AddRef();
                if (!handle.dirty && !alwaysLoadCheck)
                    return true;
                // Check Dirty
                if (f_fileSystem == null)
                    f_fileSystem = GameUty.FileSystem;
                if (f_fileSystem.IsExistentFile(key))
                {
                    using (var f = GameUty.FileOpen(key, f_fileSystem))
                    {
                        if (f != null && !f.IsValid())
                        {
                            int size = f.GetSize();
                            if (handle.length == size)
                            {
                                if (ImportCM.m_texTempFile == null)
                                {
                                    ImportCM.m_texTempFile = new byte[Math.Max(500000, size)];
                                }
                                else if (ImportCM.m_texTempFile.Length < size)
                                {
                                    ImportCM.m_texTempFile = new byte[size];
                                }
                                f.Read(ref ImportCM.m_texTempFile, size);
                                if (handle.hash == GraphicPatch.XXHash3_64(ImportCM.m_texTempFile, size))
                                {
                                    handle.dirty = false;
                                    return true;
                                }
                            }
                        }
                    }
                }
                handle.Release();
                return false;
            }

            public void MarkDirty(string key)
            {
                if (cache.TryGetValue(key, out var h) && h != null)
                    h.dirty = true;
            }

            public void SetAllDirty()
            {
                foreach (var h in cache.Values)
                {
                    if (h != null)
                        h.dirty = true;
                }
            }

            public bool Release(Texture2D tex, bool immediate = false)
            {
                if (tex == null)
                    return true;
                if (reverseCache.TryGetValue(tex, out var h))
                {
                    if (h.Release(immediate))
                    {
                        reverseCache.Remove(tex);
                        cache.Remove(h.objName);
                    }
                    return false;
                }
                return true;
            }
        }

        internal class TryPatchMaidLoader : TryPatch
        {
            public TryPatchMaidLoader(Harmony harmony, int failLimit = 5) : base(harmony, failLimit) { }

            public override bool Patch()
            {
                var mOriginal = AccessTools.Method(AccessTools.TypeByName("COM3D2.MaidLoader.RefreshMod"), "RefreshCo");
                var mPrefix = AccessTools.Method(typeof(TextureCache), nameof(SetAllDirty));
                harmony.Patch(mOriginal, prefix: new HarmonyMethod(mPrefix));
                return true;
            }
        }

#if TEXTURECACHE_DEBUG
        internal static class TextureCacheDebug
        {
            static GameObject gameObject;

            public static void Init()
            {
                gameObject = new GameObject();
                gameObject.name = "TextureCacheDebug";
                gameObject.AddComponent<TextureCacheDebugGUI>();
            }

            public static void UnInit()
            {
                GameObject.Destroy(gameObject);
                gameObject = null;
            }

            class TextureCacheDebugGUI : MonoBehaviour
            {
                private static bool showGUI = false;
                private GUIStyle grayCS;
                private GUIStyle whiteCS;
                private KeyCode ENABLE_KEYCODE = KeyCode.D;
                private Vector2 scrollViewPos = Vector2.zero;
                private const int WIDTH = 1080;
                private const int HEIGHT = 720;
                private const int MARGIN_X = 5;
                private const int MARGIN_TOP = 20;
                private const int MARGIN_BOTTOM = 5;

                void Awake()
                {
                    DontDestroyOnLoad(this);
                    grayCS = new GUIStyle();
                    grayCS.normal.textColor = new Color(0.7f, 0.7f, 0.7f);
                    grayCS.font = Font.CreateDynamicFontFromOSFont("Consolas", 16);
                    whiteCS = new GUIStyle();
                    whiteCS.normal.textColor = Color.white;
                    whiteCS.font = Font.CreateDynamicFontFromOSFont("Consolas", 16);
                }

                void Update()
                {
#pragma warning disable ULib004
                    if (Input.GetKeyDown(ENABLE_KEYCODE) && Input.GetKey(KeyCode.LeftControl))
                    {
                        showGUI = !showGUI;
                    }
#pragma warning restore ULib004
                }

                void Window(int id)
                {
                    GUILayout.BeginArea(new Rect(MARGIN_X, MARGIN_TOP, WIDTH - MARGIN_X * 2, HEIGHT - MARGIN_TOP - MARGIN_BOTTOM));
                    {
                        scrollViewPos = GUILayout.BeginScrollView(scrollViewPos);
                        {
                            GUILayout.BeginVertical();
                            {
                                GUILayout.Label("TexCache", whiteCS);
                                GUILayout.Label("refCount".PadRight(10) + "texName".PadRight(80) + "ID".PadRight(10) + "isDirt".PadRight(10), whiteCS);
                                int i = 0;
                                foreach (var p in texCache.cache)
                                {
                                    if (p.Key.EndsWith("_i_.tex") || p.Key.StartsWith("_i_") || p.Key.Contains("_icon_"))
                                    {
                                        continue;
                                    }
                                    GUILayout.Label(p.Value.refCount.ToString().PadRight(10) + p.Key.PadRight(80) + p.Value.obj.GetInstanceID().ToString().PadRight(10) + p.Value.dirty.ToString().PadRight(10), (i++ % 2 == 0) ? grayCS : whiteCS);
                                }
                                GUI.enabled = true;
                            }
                            GUILayout.EndVertical();
                        }
                        GUILayout.EndScrollView();
                    }
                    GUILayout.EndArea();
                }

                void OnGUI()
                {
                    if (!showGUI)
                    {
                        return;
                    }
                    GUI.Window(11452, new Rect(0, (Screen.height - HEIGHT) / 2f, WIDTH, HEIGHT), Window, "TextureCache Debug");
                }
            }
        }
#endif
    }
}
