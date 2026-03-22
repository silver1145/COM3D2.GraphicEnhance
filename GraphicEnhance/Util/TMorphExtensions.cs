using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace COM3D2.GraphicEnhance.Util
{
    internal class BatchBlendData
    {
        public IntPtr morph_offsets;
        public IntPtr morph_block_starts;
        public IntPtr v_indices;
        public IntPtr deltas_pos;
        public IntPtr deltas_norm;
        public IntPtr last_weights_pos;
        // Some plugins use TMorph.FixBlendValues on Face, which lead to incorrect blending in `Delta` mode. To avoid this, we need to store separate last weights for pos and norm.
        public IntPtr last_weights_pos_norm;
        public IntPtr delta_weights;
        public IntPtr active_buf;
        public bool full_sync_flag;
        public uint nnz;
        public uint vertex_count;
        public uint morph_count;
        public uint batch_size;

        public void Free()
        {
            FreePtr(ref morph_offsets);
            FreePtr(ref morph_block_starts);
            FreePtr(ref v_indices);
            FreePtr(ref deltas_pos);
            FreePtr(ref deltas_norm);
            FreePtr(ref last_weights_pos);
            FreePtr(ref last_weights_pos_norm);
            FreePtr(ref delta_weights);
            FreePtr(ref active_buf);
            nnz = 0;
            vertex_count = 0;
            morph_count = 0;
        }

        private static void FreePtr(ref IntPtr ptr)
        {
            if (ptr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(ptr);
                ptr = IntPtr.Zero;
            }
        }
    }

    internal static class TMorphExtensions
    {
        private const int BLOCK_SHIFT = 8;

        public static unsafe BatchBlendData ConvertBlendData(this TMorph morph, bool delta)
        {
            List<BlendData> blendDatas = morph.BlendDatas;
            int morphCount = morph.MorphCount;
            int vertexCount = morph.VCount;
            int numBlocks = (vertexCount >> BLOCK_SHIFT) + 1;

            IntPtr pMorphOffsets = Marshal.AllocHGlobal((morphCount + 1) * sizeof(uint));
            uint* mo = (uint*)pMorphOffsets;
            mo[0] = 0;
            for (int j = 0; j < morphCount; j++)
            {
                int count = 0;
                if (blendDatas[j] != null && blendDatas[j].v_index != null)
                    count = blendDatas[j].v_index.Length;
                mo[j + 1] = mo[j] + (uint)count;
            }
            uint nnz = mo[morphCount];

            IntPtr pVIndices = Marshal.AllocHGlobal((int)nnz * sizeof(uint));
            IntPtr pDeltasPos = Marshal.AllocHGlobal((int)nnz * 3 * sizeof(float));
            IntPtr pDeltasNorm = Marshal.AllocHGlobal((int)nnz * 3 * sizeof(float));

            uint* vi = (uint*)pVIndices;
            float* dp = (float*)pDeltasPos;
            float* dn = (float*)pDeltasNorm;

            for (int j = 0; j < morphCount; j++)
            {
                if (blendDatas[j] == null || blendDatas[j].v_index == null)
                    continue;

                int entryCount = blendDatas[j].v_index.Length;
                int baseIdx = (int)mo[j];

                int[] sortedOrder = new int[entryCount];
                for (int k = 0; k < entryCount; k++)
                    sortedOrder[k] = k;
                int[] vIndex = blendDatas[j].v_index;
                Array.Sort(sortedOrder, (a, b) => vIndex[a].CompareTo(vIndex[b]));

                for (int k = 0; k < entryCount; k++)
                {
                    int origK = sortedOrder[k];
                    int writePos = baseIdx + k;
                    vi[writePos] = (uint)vIndex[origK];

                    dp[writePos * 3 + 0] = blendDatas[j].vert[origK].x;
                    dp[writePos * 3 + 1] = blendDatas[j].vert[origK].y;
                    dp[writePos * 3 + 2] = blendDatas[j].vert[origK].z;
                    dn[writePos * 3 + 0] = blendDatas[j].norm[origK].x;
                    dn[writePos * 3 + 1] = blendDatas[j].norm[origK].y;
                    dn[writePos * 3 + 2] = blendDatas[j].norm[origK].z;
                }
            }

            IntPtr pMorphBlockStarts = Marshal.AllocHGlobal(morphCount * numBlocks * sizeof(uint));
            uint* mbs = (uint*)pMorphBlockStarts;

            for (int m = 0; m < morphCount; m++)
            {
                uint mStart = mo[m];
                uint mEnd = mo[m + 1];
                int entryIdx = (int)mStart;
                for (int b = 0; b < numBlocks; b++)
                {
                    uint blockStartVertex = (uint)(b << BLOCK_SHIFT);
                    while (entryIdx < (int)mEnd && vi[entryIdx] < blockStartVertex)
                        entryIdx++;
                    mbs[m * numBlocks + b] = (uint)entryIdx;
                }
            }

            IntPtr pLastWeightsPos = IntPtr.Zero;
            IntPtr pLastWeightsPosNorm = IntPtr.Zero;
            IntPtr pDeltaWeights = IntPtr.Zero;
            IntPtr pActiveBuf = Marshal.AllocHGlobal(morphCount * sizeof(uint));
            if (delta)
            {
                int weightsBytes = morphCount * sizeof(float);
                pLastWeightsPos = Marshal.AllocHGlobal(weightsBytes);
                if (ShapekeyEnhance.deltaBlendPosNormFix)
                    pLastWeightsPosNorm = Marshal.AllocHGlobal(weightsBytes);
                pDeltaWeights = Marshal.AllocHGlobal(weightsBytes);
                float* lwp = (float*)pLastWeightsPos;
                float* lwpn = (float*)pLastWeightsPosNorm;
                float* dw = (float*)pDeltaWeights;
                for (int i = 0; i < morphCount; i++)
                {
                    lwp[i] = 0f;
                    lwpn[i] = 0f;
                    dw[i] = 0f;
                }
            }

            return new BatchBlendData
            {
                morph_offsets = pMorphOffsets,
                morph_block_starts = pMorphBlockStarts,
                v_indices = pVIndices,
                deltas_pos = pDeltasPos,
                deltas_norm = pDeltasNorm,
                last_weights_pos = pLastWeightsPos,
                last_weights_pos_norm = pLastWeightsPosNorm,
                delta_weights = pDeltaWeights,
                active_buf = pActiveBuf,
                full_sync_flag = ShapekeyEnhance.deltaFullSyncFlag,
                nnz = nnz,
                vertex_count = (uint)vertexCount,
                morph_count = (uint)morphCount,
                batch_size = CalcBatchSize(vertexCount)
            };
        }

        public static void FreeBlendDataSparse(this TMorph morph)
        {
            List<BlendData> blendDatas = morph.BlendDatas;
            int morphCount = morph.MorphCount;
            for (int j = 0; j < morphCount; j++)
            {
                if (blendDatas[j] == null)
                    continue;
                blendDatas[j].v_index = null;
                blendDatas[j].vert = null;
                blendDatas[j].norm = null;
            }
        }

        private static uint CalcBatchSize(int vertexCount)
        {
            int poolSize = GraphicPatch.threadPoolSize;
            uint batchSize = 1024;
            while (batchSize * 2 * poolSize <= vertexCount)
            {
                batchSize *= 2;
            }
            return batchSize;
        }
    }
}
