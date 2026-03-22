using COM3D2.GraphicEnhance.Util;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;
#if SHAPEKEYENHANCE_DEBUG
using System.Diagnostics;
#endif

namespace COM3D2.GraphicEnhance
{
    public enum ShapekeyMethod
    {
        Full = 0,
        Delta = 1,
    }

    internal class ShapekeyEnhance
    {
        internal static bool globalEnable = true;
        internal static ShapekeyMethod shapekeyMethod = ShapekeyMethod.Delta;
        internal static bool deltaBlendPosNormFix = true;
        internal static bool deltaFullSyncFlag = false;
        internal static int deltaFullSyncInterval = 60;
        internal static int lastSyncTime = 0;
        internal static Dictionary<TMorph, BatchBlendData> morphBatchBlendData = new Dictionary<TMorph, BatchBlendData>();

        public static void ConvertBoneData(TMorph morph)
        {
            if (globalEnable)
            {
                morphBatchBlendData[morph] = morph.ConvertBlendData(shapekeyMethod == ShapekeyMethod.Delta);
                morph.FreeBlendDataSparse();
            }
        }

        public static void CleanWeightData(TMorph morph)
        {
            if (!globalEnable || morph.MorphCount == 0)
            {
                return;
            }
            float[] blendValues = morph.BlendValues;
            for (int i = 0; i < blendValues.Length; i++)
            {
                if (blendValues[i] < 0.01f)
                {
                    blendValues[i] = 0;
                }
            }
            List<BlendData> blendDatas = morph.BlendDatas;
            for (int j = 0; j < morph.MorphCount; j++)
            {
                if (blendDatas[j] == null)
                {
                    morph.UruUruScaleX = blendValues[j];
                }
            }
        }

        public static unsafe bool BlendShapeKeyPos(TMorph morph)
        {
            BatchBlendData batchBlendData;
            if (!morphBatchBlendData.TryGetValue(morph, out batchBlendData))
            {
                if (!globalEnable)
                {
                    morph.m_vOriVert.CopyTo(morph.m_vTmpVert, 0);
                    return false;
                }
                morphBatchBlendData[morph] = batchBlendData = morph.ConvertBlendData(shapekeyMethod == ShapekeyMethod.Delta);
                morph.FreeBlendDataSparse();
            }
            IntPtr deltaWeights = batchBlendData.delta_weights;
            if (batchBlendData.full_sync_flag != deltaFullSyncFlag)
            {
                batchBlendData.full_sync_flag = deltaFullSyncFlag;
                deltaWeights = IntPtr.Zero;
            }
            fixed (float* weights = morph.BlendValues) fixed (Vector3* base_verts = morph.m_vOriVert) fixed (Vector3* out_verts = morph.m_vTmpVert)
            {
                GraphicPatch.BlendShapePosBatch(
                    batchBlendData.morph_offsets,
                    batchBlendData.morph_block_starts,
                    batchBlendData.v_indices,
                    batchBlendData.deltas_pos,
                    (IntPtr)weights,
                    batchBlendData.last_weights_pos,
                    deltaWeights,
                    batchBlendData.active_buf,
                    (IntPtr)base_verts,
                    (IntPtr)out_verts,
                    batchBlendData.vertex_count,
                    batchBlendData.morph_count,
                    batchBlendData.batch_size
                );
            }
            return true;
        }

        public static unsafe bool BlendShapeKeyPosNormal(TMorph morph)
        {
            BatchBlendData batchBlendData;
            if (!morphBatchBlendData.TryGetValue(morph, out batchBlendData))
            {
                if (!globalEnable)
                {
                    morph.m_vOriVert.CopyTo(morph.m_vTmpVert, 0);
                    morph.m_vOriNorm.CopyTo(morph.m_vTmpNorm, 0);
                    return false;
                }
                morphBatchBlendData[morph] = batchBlendData = morph.ConvertBlendData(shapekeyMethod == ShapekeyMethod.Delta);
                morph.FreeBlendDataSparse();
            }
            IntPtr deltaWeights = batchBlendData.delta_weights;
            if (batchBlendData.full_sync_flag != deltaFullSyncFlag)
            {
                batchBlendData.full_sync_flag = deltaFullSyncFlag;
                deltaWeights = IntPtr.Zero;
            }
            fixed (float* weights = morph.BlendValues) fixed (Vector3* base_verts = morph.m_vOriVert) fixed (Vector3* out_verts = morph.m_vTmpVert) fixed (Vector3* base_norms = morph.m_vOriNorm) fixed (Vector3* out_norms = morph.m_vTmpNorm)
            {
                GraphicPatch.BlendShapePosNormBatch(
                    batchBlendData.morph_offsets,
                    batchBlendData.morph_block_starts,
                    batchBlendData.v_indices,
                    batchBlendData.deltas_pos,
                    batchBlendData.deltas_norm,
                    (IntPtr)weights,
                    batchBlendData.last_weights_pos,
                    batchBlendData.last_weights_pos_norm,
                    deltaWeights,
                    batchBlendData.active_buf,
                    (IntPtr)base_verts,
                    (IntPtr)base_norms,
                    (IntPtr)out_verts,
                    (IntPtr)out_norms,
                    batchBlendData.vertex_count,
                    batchBlendData.morph_count,
                    batchBlendData.batch_size
                );
            }
            return true;
        }

        [HarmonyPatch(typeof(TBodySkin), nameof(TBodySkin.Load), new Type[] { typeof(MPN), typeof(Transform), typeof(Transform), typeof(Dictionary<string, Transform>), typeof(string), typeof(string), typeof(string), typeof(string), typeof(int), typeof(bool), typeof(int) })]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> TBodySkinLoad(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            CodeMatcher codeMatcher = new CodeMatcher(instructions, generator);
            codeMatcher.Start()
                .MatchForward(false, new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(TMorph), nameof(TMorph.InitGameObject))))
                .Advance(1)
                .InsertAndAdvance(codeMatcher.InstructionsWithOffsets(-4, -3))
                .InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ShapekeyEnhance), nameof(ConvertBoneData))));
            return codeMatcher.InstructionEnumeration();
        }

        [HarmonyPatch(typeof(TMorph), nameof(TMorph.DeleteObj))]
        [HarmonyPostfix]
        private static void TMorphDeleteObjPostfix(TMorph __instance)
        {
            BatchBlendData batchBlendData;
            if (morphBatchBlendData.TryGetValue(__instance, out batchBlendData))
            {
                batchBlendData.Free();
                morphBatchBlendData.Remove(__instance);
            }
        }

        // `TMorph.FixBlendValues_Face`
        /*
+       ShapekeyEnhance.CleanWeightData(this);
        ...
-       this.m_vOriVert.CopyTo(this.m_vTmpVert, 0);
        ...
+       if (!ShapekeyEnhance.BlendShapeKeyPos(this))
+       {
            for (int j = 0; j < this.MorphCount; j++)
            {
            ...
            }
+       }
        this.m_mesh.vertices = this.m_vTmpVert;
        */
        [HarmonyPatch(typeof(TMorph), nameof(TMorph.FixBlendValues_Face))]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> FixBlendValuesFaceTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var copyToMethod = AccessTools.Method(typeof(Array), nameof(Array.CopyTo), new[] { typeof(Array), typeof(int) });
            CodeMatcher codeMatcher = new CodeMatcher(instructions, generator);
            codeMatcher.Start()
                .InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0))
                .InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ShapekeyEnhance), nameof(CleanWeightData))));
            codeMatcher.Start()
                .MatchForward(false, new CodeMatch(OpCodes.Callvirt, copyToMethod))
                .Advance(-5);
            {
                var labels = new List<Label>();
                for (int i = 0; i < 6; i++)
                    labels.AddRange(codeMatcher.InstructionAt(i).labels);
                codeMatcher.RemoveInstructions(6);
                codeMatcher.Instruction.labels.AddRange(labels);
            }
            Label skipLabel;
            codeMatcher.Start()
                .MatchForward(false, new CodeMatch(OpCodes.Callvirt, AccessTools.PropertySetter(typeof(Mesh), nameof(Mesh.vertices))))
                .Advance(-4)
                .CreateLabel(out skipLabel)
                .MatchBack(false, new CodeMatch(OpCodes.Ldc_I4_0))
                .Advance(-1)
                .MatchBack(false, new CodeMatch(OpCodes.Ldc_I4_0));
            var forLoopLabels = new List<Label>(codeMatcher.Instruction.labels);
            codeMatcher.Instruction.labels.Clear();
            var ldarg0 = new CodeInstruction(OpCodes.Ldarg_0);
            ldarg0.labels.AddRange(forLoopLabels);
            codeMatcher.InsertAndAdvance(ldarg0)
                .InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ShapekeyEnhance), nameof(BlendShapeKeyPos))))
                .InsertAndAdvance(new CodeInstruction(OpCodes.Brtrue, skipLabel));
            return codeMatcher.InstructionEnumeration();
        }

        // `TMorph.FixBlendValues`
        /*
+       ShapekeyEnhance.CleanWeightData(this);
        ...
-       this.m_vOriVert.CopyTo(this.m_vTmpVert, 0);
-       this.m_vOriNorm.CopyTo(this.m_vTmpNorm, 0);
        ...
+       if (!ShapekeyEnhance.BlendShapeKeyPosNorm(this))
+       {
            for (int j = 0; j < this.MorphCount; j++)
            {
            ...
            }
+       }
        this.m_mesh.vertices = this.m_vTmpVert;
        */
        [HarmonyPatch(typeof(TMorph), nameof(TMorph.FixBlendValues))]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> FixBlendValuesTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var copyToMethod = AccessTools.Method(typeof(Array), "CopyTo", new[] { typeof(Array), typeof(int) });
            CodeMatcher codeMatcher = new CodeMatcher(instructions, generator);
            codeMatcher.Start()
                .InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0))
                .InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ShapekeyEnhance), nameof(CleanWeightData))));
            codeMatcher.Start()
                .MatchForward(false, new CodeMatch(OpCodes.Callvirt, copyToMethod))
                .Advance(-5);
            {
                var labels = new List<Label>();
                for (int i = 0; i < 6; i++)
                    labels.AddRange(codeMatcher.InstructionAt(i).labels);
                codeMatcher.RemoveInstructions(6);
                codeMatcher.Instruction.labels.AddRange(labels);
            }
            codeMatcher.MatchForward(false, new CodeMatch(OpCodes.Callvirt, copyToMethod))
                .Advance(-5);
            {
                var labels = new List<Label>();
                for (int i = 0; i < 6; i++)
                    labels.AddRange(codeMatcher.InstructionAt(i).labels);
                codeMatcher.RemoveInstructions(6);
                codeMatcher.Instruction.labels.AddRange(labels);
            }
            Label skipLabel;
            codeMatcher.Start()
                .MatchForward(false, new CodeMatch(OpCodes.Callvirt, AccessTools.PropertySetter(typeof(Mesh), "vertices")))
                .Advance(-4)
                .CreateLabel(out skipLabel)
                .MatchBack(false, new CodeMatch(OpCodes.Ldc_I4_0))
                .Advance(-1)
                .MatchBack(false, new CodeMatch(OpCodes.Ldc_I4_0));
            var forLoopLabels = new List<Label>(codeMatcher.Instruction.labels);
            codeMatcher.Instruction.labels.Clear();
            var ldarg0 = new CodeInstruction(OpCodes.Ldarg_0);
            ldarg0.labels.AddRange(forLoopLabels);
            codeMatcher.InsertAndAdvance(ldarg0)
                .InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ShapekeyEnhance), nameof(BlendShapeKeyPosNormal))))
                .InsertAndAdvance(new CodeInstruction(OpCodes.Brtrue, skipLabel));
            return codeMatcher.InstructionEnumeration();
        }

#if SHAPEKEYENHANCE_DEBUG
        private static readonly Stopwatch _swBody = new Stopwatch();
        private static long _accumBody = 0;
        private static int _countBody = 0;
        private const int LogInterval = 500;

        [HarmonyPatch(typeof(TMorph), nameof(TMorph.FixBlendValues))]
        [HarmonyPrefix]
        private static void FixBlendValuesPrefix()
        {
            _swBody.Reset();
            _swBody.Start();
        }

        [HarmonyPatch(typeof(TMorph), nameof(TMorph.FixBlendValues))]
        [HarmonyPostfix]
        private static void FixBlendValuesPostfix()
        {
            _swBody.Stop();
            _accumBody += _swBody.ElapsedTicks;
            _countBody++;
            if (_countBody >= LogInterval)
            {
                double avgMs = (double)_accumBody / _countBody / Stopwatch.Frequency * 1000.0;
                GraphicEnhance.Logger.LogDebug(string.Format("FixBlendValues avg: {0:F4} ms ({1} calls)", avgMs, _countBody));
                _accumBody = 0;
                _countBody = 0;
            }
        }
#endif
    }
}
