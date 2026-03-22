#pragma once

typedef void(*CopyBoneDataFunc)(float *dest, const float *cachedPose, int boneCount);

extern "C" void CopyBoneMatrixSSE4(float *dst, const float *src, int count);

extern "C" void CopyBoneMatrixAndDecomposeSSE4(float *dst, const float *src, int count);

extern "C" void CopyBoneMatrixAVX2FMA(float *dst, const float *src, int count);

extern "C" void CopyBoneMatrixAndDecomposeAVX2FMA(float *dst, const float *src, int count);

bool patchInputSpec(uintptr_t hookPoint);

bool patchGetMemExShader(uintptr_t hookPoint);

bool patchUpdateSourceBones(uintptr_t hookPoint);

extern "C" __declspec(dllexport) void SkinPatch_SetSwitchDqsOn(bool on);
