#include "pch.h"
#include "BlendShape.h"
#include "util.h"

static BlendShapePosBatchFn g_BlendShapePosBatch = cpuHasAvx2Fma ? BlendShapePosApplyBatchAVX2FMA : BlendShapePosApplyBatchSSE4;

static BlendShapePosNormBatchFn g_BlendShapePosNormBatch = cpuHasAvx2Fma ? BlendShapePosNormApplyBatchAVX2FMA : BlendShapePosNormApplyBatchSSE4;

void BlendShapePosBatch(
    void *pool,
    const uint32_t *morph_offsets,
    const uint32_t *morph_block_starts,
    const uint32_t *v_indices,
    const float *deltas_pos,
    const float *weights,
    float       *last_weights,
    float       *delta_weights,
    uint32_t    *active_buf,
    const float *base_verts,
    float       *out_verts,
    uint32_t vertex_count,
    uint32_t morph_count,
    uint32_t batch_size)
{
    g_BlendShapePosBatch(pool, morph_offsets, morph_block_starts, v_indices, deltas_pos, weights, last_weights, delta_weights, active_buf, base_verts, out_verts, vertex_count, morph_count, batch_size);
}

void BlendShapePosNormBatch(
    void *pool,
    const uint32_t *morph_offsets,
    const uint32_t *morph_block_starts,
    const uint32_t *v_indices,
    const float *deltas_pos,
    const float *deltas_norm,
    const float *weights,
    float       *last_weights_pos,
    float       *last_weights_pos_norm,
    float       *delta_weights,
    uint32_t    *active_buf,
    const float *base_verts,
    const float *base_norms,
    float       *out_verts,
    float       *out_norms,
    uint32_t vertex_count,
    uint32_t morph_count,
    uint32_t batch_size)
{
    g_BlendShapePosNormBatch(pool, morph_offsets, morph_block_starts, v_indices, deltas_pos, deltas_norm, weights, last_weights_pos, last_weights_pos_norm, delta_weights, active_buf, base_verts, base_norms, out_verts, out_norms, vertex_count, morph_count, batch_size);
}
