using HarmonyLib;
using System;
using UnityEngine;

namespace COM3D2.GraphicEnhance.Util
{
    public static class Texture2DExtensions
    {
        const ulong readableFormatMask =
            (1UL << (int)TextureFormat.Alpha8) |
            (1UL << (int)TextureFormat.RGB24) |
            (1UL << (int)TextureFormat.RGBA32) |
            (1UL << (int)TextureFormat.ARGB32) |
            (1UL << (int)TextureFormat.BGRA32) |
            (1UL << (int)TextureFormat.R8) |
            (1UL << (int)TextureFormat.R16) |
            (1UL << (int)TextureFormat.RG16) |
            (1UL << (int)TextureFormat.RHalf) |
            (1UL << (int)TextureFormat.RGHalf) |
            (1UL << (int)TextureFormat.RGBAHalf) |
            (1UL << (int)TextureFormat.RFloat) |
            (1UL << (int)TextureFormat.RGFloat) |
            (1UL << (int)TextureFormat.RGBAFloat) |
            (1UL << (int)TextureFormat.RGB9e5Float);
        private static readonly AccessTools.FieldRef<UnityEngine.Object, IntPtr> cachedPtrRef = AccessTools.FieldRefAccess<UnityEngine.Object, IntPtr>("m_CachedPtr");

        public static void CopyToMem(this Texture2D tex)
        {
            if (tex == null || ((readableFormatMask >> (int)tex.format) & 1UL) == 0)
                return;
            IntPtr nativePtr = cachedPtrRef(tex);
            if (GraphicPatch.Texture2DReInit(nativePtr))
            {
                RenderTexture temp = RenderTexture.GetTemporary(
                    tex.width,
                    tex.height,
                    0,
                    RenderTextureFormat.ARGB32
                );
                Graphics.Blit(tex, temp);
                RenderTexture rt = RenderTexture.active;
                RenderTexture.active = temp;
                tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0, false);
                RenderTexture.active = rt;
                RenderTexture.ReleaseTemporary(temp);
            }
        }
    }
}
