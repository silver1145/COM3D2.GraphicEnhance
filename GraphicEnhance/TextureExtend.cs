using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Emit;
using System.Text;
using UnityEngine;

namespace COM3D2.GraphicEnhance
{
    internal class TextureExtend
    {
        internal static bool globalEnable = false;
        internal static string texTempFileName { get; set; }
        private static readonly byte[] CM3D2_TEX_MAGIC = { 0x09, 0x43, 0x4D, 0x33, 0x44, 0x32, 0x5F, 0x54, 0x45, 0x58 };

        internal static void GetBlockSize(TextureFormat format, out int bw, out int bh)
        {
            switch (format)
            {
                case TextureFormat.DXT1:
                case TextureFormat.DXT5:
                case TextureFormat.DXT1Crunched:
                case TextureFormat.DXT5Crunched:
                case TextureFormat.BC4:
                case TextureFormat.BC5:
                case TextureFormat.BC6H:
                case TextureFormat.BC7:
                case TextureFormat.ETC_RGB4:
                case TextureFormat.ETC2_RGB:
                case TextureFormat.ETC2_RGBA1:
                case TextureFormat.ETC2_RGBA8:
                case TextureFormat.EAC_R:
                case TextureFormat.EAC_R_SIGNED:
                case TextureFormat.EAC_RG:
                case TextureFormat.EAC_RG_SIGNED:
                case TextureFormat.PVRTC_RGB4:
                case TextureFormat.PVRTC_RGBA4:
                case TextureFormat.ATC_RGB4:
                case TextureFormat.ATC_RGBA8:
                case TextureFormat.ETC_RGB4_3DS:
                case TextureFormat.ETC_RGBA8_3DS:
                case TextureFormat.ASTC_RGB_4x4:
                case TextureFormat.ASTC_RGBA_4x4:
                    bw = 4; bh = 4; return;
                case TextureFormat.ASTC_RGB_5x5:
                case TextureFormat.ASTC_RGBA_5x5:
                    bw = 5; bh = 5; return;
                case TextureFormat.ASTC_RGB_6x6:
                case TextureFormat.ASTC_RGBA_6x6:
                    bw = 6; bh = 6; return;
                case TextureFormat.PVRTC_RGB2:
                case TextureFormat.PVRTC_RGBA2:
                    bw = 8; bh = 4; return;
                case TextureFormat.ASTC_RGB_8x8:
                case TextureFormat.ASTC_RGBA_8x8:
                    bw = 8; bh = 8; return;
                case TextureFormat.ASTC_RGB_10x10:
                case TextureFormat.ASTC_RGBA_10x10:
                    bw = 10; bh = 10; return;
                case TextureFormat.ASTC_RGB_12x12:
                case TextureFormat.ASTC_RGBA_12x12:
                    bw = 12; bh = 12; return;
                default:
                    bw = 1; bh = 1; return;
            }
        }

        internal static int Align(int size, int blockSize)
        {
            if ((blockSize & (blockSize - 1)) == 0)
                return (size + blockSize - 1) & -blockSize;
            return ((size + blockSize - 1) / blockSize) * blockSize;
        }

        internal static Texture2D CreateTexture2D(TextureResource res)
        {
#pragma warning disable ULib009
            bool mipmap = false;
            bool linear = false;
            if (texTempFileName != null)
            {
                mipmap = texTempFileName.Contains("mipmap");
                linear = texTempFileName.Contains("linear");
            }
            texTempFileName = null;
            int width = res.width;
            int height = res.height;
            TextureFormat format = res.format;
            byte[] data = res.data;
            GetBlockSize(format, out int bw, out int bh);
            if (format == TextureFormat.ARGB32 || format == TextureFormat.RGB24)
            {
                Texture2D tex = new Texture2D(2, 2, format, mipmap, linear);
                tex.LoadImage(data);
                return tex;
            }
            int alignedW = Align(width, bw);
            int alignedH = Align(height, bh);
            if (width == alignedW && height == alignedH)
            {
                Texture2D tex = new Texture2D(width, height, format, mipmap, linear);
                tex.LoadRawTextureData(data);
                tex.Apply();
                return tex;
            }
            Texture2D padded = new Texture2D(alignedW, alignedH, format, false, linear);
            if (data.Length < padded.GetRawTextureData().Length)
            {
                Debug.LogError("paddedTextureのサイズが不正。");
                UnityEngine.Object.Destroy(padded);
                return null;
            }
            padded.LoadRawTextureData(data);
            padded.Apply();
            RenderTexture rt = RenderTexture.active;
            bool lastSRGBWrite = GL.sRGBWrite;
            GL.sRGBWrite = !linear;
            RenderTexture temp = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(padded, temp);
            RenderTexture.active = temp;
            GL.sRGBWrite = lastSRGBWrite;
            Texture2D result = new Texture2D(width, height, TextureFormat.RGBA32, mipmap, linear);
            result.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            result.Apply();
            RenderTexture.ReleaseTemporary(temp);
            UnityEngine.Object.Destroy(padded);
            RenderTexture.active = rt;
            return result;
#pragma warning restore ULib009
        }

        internal static BinaryReader TrySeekToExtendedTex(BinaryReader reader)
        {
            if (!globalEnable) return reader;

            Stream stream = reader.BaseStream;
            long startPos = stream.Position;
            try
            {
                string header = reader.ReadString();
                if (header != "CM3D2_TEX")
                {
                    stream.Position = startPos;
                    return reader;
                }
                int version = reader.ReadInt32();
                reader.ReadString();
                if (1010 <= version)
                {
                    if (1011 <= version)
                    {
                        int rectCount = reader.ReadInt32();
                        if (0 < rectCount)
                            stream.Position += rectCount * 16L;
                    }
                    stream.Position += 12;
                }
                int dataSize = reader.ReadInt32();
                stream.Position += dataSize;
                long secondPos = stream.Position;
                if (secondPos + CM3D2_TEX_MAGIC.Length <= stream.Length)
                {
                    bool match = true;
                    for (int i = 0; i < CM3D2_TEX_MAGIC.Length; i++)
                    {
                        if (stream.ReadByte() != CM3D2_TEX_MAGIC[i])
                        {
                            match = false;
                            break;
                        }
                    }
                    if (match)
                    {
                        stream.Position = secondPos;
                        return reader;
                    }
                }
            }
            catch { }
            stream.Position = startPos;
            return reader;
        }

        [HarmonyPatch(typeof(TextureResource), nameof(TextureResource.CreateTexture2D))]
        [HarmonyTranspiler]
        internal static IEnumerable<CodeInstruction> CreateTexture2DTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            Label label;
            CodeMatcher codeMatcher = new CodeMatcher(instructions, generator);
            codeMatcher.Start()
                .CreateLabel(out label)
                .InsertAndAdvance(new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(TextureExtend), nameof(globalEnable))))
                .InsertAndAdvance(new CodeInstruction(OpCodes.Brfalse_S, label))
                .InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0))
                .InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TextureExtend), nameof(CreateTexture2D))))
                .InsertAndAdvance(new CodeInstruction(OpCodes.Ret));
            return codeMatcher.InstructionEnumeration();
        }

        [HarmonyPatch(typeof(ImportCM), nameof(ImportCM.LoadTexture))]
        [HarmonyPostfix]
        internal static void LoadTexturePostfix(string f_strFileName)
        {
            if (f_strFileName != null)
                texTempFileName = f_strFileName.ToLower();
        }

        // `TBody.ChangeTex`
        /*
        obj.transform.GetComponentsInChildren<Renderer>(true, list);
+       TextureExtend.set_texTempFileName(filename);
        */
        [HarmonyPatch(typeof(TBody), nameof(TBody.ChangeTex))]
        [HarmonyTranspiler]
        internal static IEnumerable<CodeInstruction> ChangeTexTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            CodeMatcher codeMatcher = new CodeMatcher(instructions, generator);
            codeMatcher.Start()
                .MatchForward(false, new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(string), nameof(string.ToLower))));
            var loadFilenameField = codeMatcher.InstructionsWithOffsets(-2, -1);
            codeMatcher
                .MatchForward(false, new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(Component), nameof(Component.GetComponentsInChildren))))
                .Advance(1)
                .InsertAndAdvance(loadFilenameField)
                .InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.PropertySetter(typeof(TextureExtend), nameof(texTempFileName))));
            return codeMatcher.InstructionEnumeration();
        }

        // `ImportCM.LoadTextureFile`
        /*
        BinaryReader binaryReader = new BinaryReader(new MemoryStream(ImportCM.m_texTempFile), Encoding.UTF8);
+       TextureExtend.TrySeekToExtendedTex(binaryReader);
        */
        [HarmonyPatch(typeof(ImportCM), nameof(ImportCM.LoadTextureFile))]
        [HarmonyTranspiler]
        internal static IEnumerable<CodeInstruction> LoadTextureFileTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            CodeMatcher codeMatcher = new CodeMatcher(instructions, generator);
            codeMatcher.Start()
                .MatchForward(false, new CodeMatch(OpCodes.Newobj, AccessTools.Constructor(typeof(BinaryReader), new Type[] { typeof(Stream), typeof(Encoding) })))
                .Advance(1)
                .InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TextureExtend), nameof(TrySeekToExtendedTex))));
            return codeMatcher.InstructionEnumeration();
        }
    }
}
