using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;

namespace COM3D2.GraphicEnhance
{
    public class GraphicPatch
    {
        static IntPtr hModule = IntPtr.Zero;
        static string LibPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "GraphicPatch.dll");
        static IntPtr threadPool = IntPtr.Zero;
        internal static int threadPoolSize = 1;

        [DllImport("Kernel32.dll")]
        private static extern IntPtr LoadLibrary(string path);

        [DllImport("Kernel32.dll")]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        public static Delegate LoadFunction<T>(string functionName)
        {
            var functionAddress = GetProcAddress(hModule, functionName);
            if (functionAddress == IntPtr.Zero)
            {
                int error = Marshal.GetLastWin32Error();
                GraphicEnhance.Logger.LogError($"GetProcAddress Failed: {functionName}, [Win32 Error {error}] {new Win32Exception(error).Message}");
                return null;
            }
            return Marshal.GetDelegateForFunctionPointer(functionAddress, typeof(T));
        }

        private delegate bool PatchInitType();
        private delegate int GetPhysicalProcessorCountType();
        private delegate int GetLogicalProcessorCountType();
        private delegate void SkinPatch_SetSwitchDqsOnType(bool on);
        private delegate ulong XXHash3_64_Type(IntPtr data, uint len, ulong seed);
        private delegate bool Texture2DReInitType(IntPtr texture2d);
        private delegate IntPtr ThreadPoolInitType(int thread_count);
        private delegate void ThreadPoolDeinitType(IntPtr pool);
        private delegate void BlendShapePosBatchType(
            IntPtr pool,
            IntPtr morph_offsets, IntPtr morph_block_starts,
            IntPtr v_indices,
            IntPtr deltas_pos,
            IntPtr weights,
            IntPtr last_weights,
            IntPtr delta_weights,
            IntPtr active_buf,
            IntPtr base_verts,
            IntPtr out_verts,
            uint vertex_count,
            uint morph_count,
            uint batch_size);
        private delegate void BlendShapePosNormBatchType(
            IntPtr pool,
            IntPtr morph_offsets, IntPtr morph_block_starts,
            IntPtr v_indices,
            IntPtr deltas_pos,
            IntPtr deltas_norm,
            IntPtr weights,
            IntPtr last_weights_pos,
            IntPtr last_weights_pos_norm,
            IntPtr delta_weights,
            IntPtr active_buf,
            IntPtr base_verts,
            IntPtr base_norms,
            IntPtr out_verts,
            IntPtr out_norms,
            uint vertex_count,
            uint morph_count,
            uint batch_size);
        static private PatchInitType patchInit;
        static private GetPhysicalProcessorCountType getPhysicalProcessorCount;
        static private GetLogicalProcessorCountType getLogicalProcessorCount;
        static private SkinPatch_SetSwitchDqsOnType skinPatch_SetSwitchDqsOn;
        static private XXHash3_64_Type xxhash3_64;
        static private Texture2DReInitType texture2DReInit;
        static private ThreadPoolInitType threadPoolInit;
        static private ThreadPoolDeinitType threadPoolDeinit;
        static private BlendShapePosBatchType blendShapePosBatch;
        static private BlendShapePosNormBatchType blendShapePosNormBatch;

        internal static bool Init()
        {
            hModule = LoadLibrary(LibPath);
            if (hModule == IntPtr.Zero)
            {
                return false;
            }
            patchInit = (PatchInitType)LoadFunction<PatchInitType>("PatchInit");
            if (patchInit == null)
            {
                return false;
            }
            skinPatch_SetSwitchDqsOn = (SkinPatch_SetSwitchDqsOnType)LoadFunction<SkinPatch_SetSwitchDqsOnType>("SkinPatch_SetSwitchDqsOn");
            if (skinPatch_SetSwitchDqsOn == null)
            {
                return false;
            }
            getPhysicalProcessorCount = (GetPhysicalProcessorCountType)LoadFunction<GetPhysicalProcessorCountType>("GetPhysicalProcessorCount");
            if (getPhysicalProcessorCount == null)
            {
                return false;
            }
            getLogicalProcessorCount = (GetLogicalProcessorCountType)LoadFunction<GetLogicalProcessorCountType>("GetLogicalProcessorCount");
            if (getLogicalProcessorCount == null)
            {
                return false;
            }
            xxhash3_64 = (XXHash3_64_Type)LoadFunction<XXHash3_64_Type>("XXHash3_64");
            if (xxhash3_64 == null)
            {
                return false;
            }
            texture2DReInit = (Texture2DReInitType)LoadFunction<Texture2DReInitType>("Texture2DReInit");
            if (texture2DReInit == null)
            {
                return false;
            }
            threadPoolInit = (ThreadPoolInitType)LoadFunction<ThreadPoolInitType>("ThreadPoolInit");
            if (threadPoolInit == null)
            {
                return false;
            }
            threadPoolDeinit = (ThreadPoolDeinitType)LoadFunction<ThreadPoolDeinitType>("ThreadPoolDeinit");
            if (threadPoolDeinit == null)
            {
                return false;
            }
            blendShapePosBatch = (BlendShapePosBatchType)LoadFunction<BlendShapePosBatchType>("BlendShapePosBatch");
            if (blendShapePosBatch == null)
            {
                return false;
            }
            blendShapePosNormBatch = (BlendShapePosNormBatchType)LoadFunction<BlendShapePosNormBatchType>("BlendShapePosNormBatch");
            if (blendShapePosNormBatch == null)
            {
                return false;
            }
            return true;
        }

        internal static bool PatchInit()
        {
            if (patchInit != null)
            {
                return patchInit();
            }
            throw new Exception("Method `PatchInit` not Initialized");
        }

        public static int GetPhysicalProcessorCount()
        {
            if (getPhysicalProcessorCount != null)
            {
                return getPhysicalProcessorCount();
            }
            throw new Exception("Method `GetPhysicalProcessorCount` not Initialized");
        }

        public static int GetLogicalProcessorCount()
        {
            if (getLogicalProcessorCount != null)
            {
                return getLogicalProcessorCount();
            }
            throw new Exception("Method `GetLogicalProcessorCount` not Initialized");
        }

        public static void SetSwitchDqsOn(bool on)
        {
            if (skinPatch_SetSwitchDqsOn != null)
            {
                skinPatch_SetSwitchDqsOn(on);
                return;
            }
            throw new Exception("Method `SetSwitchDqsOn` not Initialized");
        }

        public static unsafe ulong XXHash3_64(byte[] data, ulong seed = 0)
        {
            if (xxhash3_64 != null)
            {
                fixed (byte* ptr = data)
                {
                    return xxhash3_64((IntPtr)ptr, (uint)data.Length, seed);
                }
            }
            throw new Exception("Method `XXHash3_64` not Initialized");
        }

        public static unsafe ulong XXHash3_64(byte[] data, int length, ulong seed = 0)
        {
            if (length > data.Length)
            {
                length = data.Length;
            }
            if (xxhash3_64 != null)
            {
                fixed (byte* ptr = data)
                {
                    return xxhash3_64((IntPtr)ptr, (uint)length, seed);
                }
            }
            throw new Exception("Method `XXHash3_64` not Initialized");
        }

        public static bool Texture2DReInit(IntPtr texture2d)
        {
            if (texture2DReInit != null)
            {
                return texture2DReInit(texture2d);
            }
            throw new Exception("Method `Texture2DReInit` not Initialized");
        }

        public static void ThreadPoolInit(int thread_count)
        {
            if (threadPool != IntPtr.Zero)
            {
                throw new Exception("ThreadPool already initialized");
            }
            if (threadPoolInit != null)
            {
                threadPoolSize = thread_count;
                threadPool = threadPoolInit(thread_count);
                return;
            }
            throw new Exception("Method `ThreadPoolInit` not Initialized");
        }

        public static void ThreadPoolDeinit()
        {
            if (threadPool == IntPtr.Zero)
            {
                throw new Exception("ThreadPool not initialized");
            }
            if (threadPoolDeinit != null)
            {
                threadPoolDeinit(threadPool);
                return;
            }
            throw new Exception("Method `ThreadPoolDeinit` not Initialized");
        }

        public static void BlendShapePosBatch(
            IntPtr morph_offsets, IntPtr morph_block_starts,
            IntPtr v_indices,
            IntPtr deltas_pos,
            IntPtr weights,
            IntPtr last_weights,
            IntPtr delta_weights,
            IntPtr active_buf,
            IntPtr base_verts,
            IntPtr out_verts,
            uint vertex_count,
            uint morph_count,
            uint batch_size)
        {
            // skip check for performance
            blendShapePosBatch(threadPool, morph_offsets, morph_block_starts, v_indices, deltas_pos, weights, last_weights, delta_weights, active_buf, base_verts, out_verts, vertex_count, morph_count, batch_size);
        }

        public static void BlendShapePosNormBatch(
            IntPtr morph_offsets, IntPtr morph_block_starts,
            IntPtr v_indices,
            IntPtr deltas_pos,
            IntPtr deltas_norm,
            IntPtr weights,
            IntPtr last_weights_pos,
            IntPtr last_weights_pos_norm,
            IntPtr delta_weights,
            IntPtr active_buf,
            IntPtr base_verts,
            IntPtr base_norms,
            IntPtr out_verts,
            IntPtr out_norms,
            uint vertex_count,
            uint morph_count,
            uint batch_size)
        {
            // skip check for performance
            blendShapePosNormBatch(threadPool, morph_offsets, morph_block_starts, v_indices, deltas_pos, deltas_norm, weights, last_weights_pos, last_weights_pos_norm, delta_weights, active_buf, base_verts, base_norms, out_verts, out_norms, vertex_count, morph_count, batch_size);
        }
    }
}
