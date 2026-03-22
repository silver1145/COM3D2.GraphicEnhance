#include "pch.h"
#include "util.h"
#include "SkinPatch.h"
#include "StreamOutShader.h"

bool g_SwitchDqsOn = false;
CopyBoneDataFunc g_CopyBoneData = CopyBoneMatrixSSE4;

// inputSpec = shaderChannelsMap + (bonesPerVertex << 16) + (g_SwitchDqsOn << 31)
bool patchInputSpec(uintptr_t hookPoint)
{
    int32_t origDisp;
    memcpy(&origDisp, (void *)(hookPoint + 8), 4);
    uintptr_t globalAddr = (hookPoint + 12) + (int64_t)origDisp;

    uint8_t *t = (uint8_t *)VirtualAlloc(NULL, 4096, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);
    if (!t)
        return false;

    uint8_t *p = t;
    uintptr_t switchAddr = (uintptr_t)&g_SwitchDqsOn;
    uintptr_t returnAddr = hookPoint + 16;

    uint8_t stub[] = { 0xC1, 0xE6, 0x10, 0x03, 0xF1, 0x48, 0xB8 };
    memcpy(p, stub, sizeof(stub));
    p += sizeof(stub);
    memcpy(p, &switchAddr, 8);
    p += 8;
    uint8_t mid[] = { 0x80, 0x38, 0x00, 0x74, 0x06, 0x81, 0xCE, 0x00, 0x00, 0x00, 0x80, 0x48, 0xB8 };
    memcpy(p, mid, sizeof(mid));
    p += sizeof(mid);
    memcpy(p, &globalAddr, 8);
    p += 8;
    uint8_t tail[] = { 0x48, 0x8B, 0x08, 0x4C, 0x8B, 0x41, 0x08, 0xFF, 0x25, 0x00, 0x00, 0x00, 0x00 };
    memcpy(p, tail, sizeof(tail));
    p += sizeof(tail);
    memcpy(p, &returnAddr, 8);

    uintptr_t tramAddr = (uintptr_t)t;
    uint8_t jmp[16] = { 0xFF, 0x25, 0x00, 0x00, 0x00, 0x00 };
    memcpy(jmp + 6, &tramAddr, 8);
    jmp[14] = jmp[15] = 0x90;

    DWORD old;
    if (!VirtualProtect((void *)hookPoint, 16, PAGE_EXECUTE_READWRITE, &old))
        goto fail;
    memcpy((void *)hookPoint, jmp, 16);
    if (!VirtualProtect((void *)hookPoint, 16, old, &old))
        goto fail;
    FlushInstructionCache(GetCurrentProcess(), (void *)hookPoint, 16);
    return true;

fail:
    VirtualFree(t, 0, MEM_RELEASE);
    return false;
}

void GetMemExShaderCode(int index, uint32_t* outSize, void** outPtr)
{
    if (g_SwitchDqsOn)
        index += 12;
    *outSize = g_StreamOutShaderSizes[index];
    *outPtr = (void*)g_StreamOutShaders[index];
}

bool patchGetMemExShader(uintptr_t hookPoint)
{
    uint8_t *t = (uint8_t *)VirtualAlloc(NULL, 4096, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);
    if (!t)
        return false;

    uint8_t *p = t;
    uint8_t stub[] = { 0x50, 0x51, 0x52, 0x41, 0x50, 0x41, 0x51, 0x41, 0x52, 0x41, 0x53, 0x41, 0x54, 0x48, 0x83, 0xEC, 0x30, 0x8B, 0x8C, 0x24, 0x58, 0x00, 0x00, 0x00, 0x48, 0x8D, 0x54, 0x24, 0x20, 0x4C, 0x8D, 0x44, 0x24, 0x28, 0x48, 0xB8 };
    memcpy(p, stub, sizeof(stub));
    p += sizeof(stub);
    uintptr_t fn = (uintptr_t)GetMemExShaderCode;
    memcpy(p, &fn, 8);
    p += 8;
    uint8_t tail[] = { 0xFF, 0xD0, 0x8B, 0x5C, 0x24, 0x20, 0x4C, 0x8B, 0x7C, 0x24, 0x28, 0x48, 0x83, 0xC4, 0x30, 0x41, 0x5C, 0x41, 0x5B, 0x41, 0x5A, 0x41, 0x59, 0x41, 0x58, 0x5A, 0x59, 0x58, 0xFF, 0x25, 0x00, 0x00, 0x00, 0x00 };
    memcpy(p, tail, sizeof(tail));
    p += sizeof(tail);
    uintptr_t ret = hookPoint + 16;
    memcpy(p, &ret, 8);

    uintptr_t tramAddr = (uintptr_t)t;
    uint8_t jmp[16] = {0xFF, 0x25, 0x00, 0x00, 0x00, 0x00};
    memcpy(jmp + 6, &tramAddr, 8);
    jmp[14] = jmp[15] = 0x90;

    DWORD old;
    if (!VirtualProtect((void *)hookPoint, 16, PAGE_EXECUTE_READWRITE, &old))
        goto fail;
    memcpy((void *)hookPoint, jmp, 16);
    if (!VirtualProtect((void *)hookPoint, 16, old, &old))
        goto fail;
    FlushInstructionCache(GetCurrentProcess(), (void *)hookPoint, 16);
    return true;

fail:
    VirtualFree(t, 0, MEM_RELEASE);
    return false;
}

static void CopyBoneData(float* dest, const float* cachedPose, int boneCount)
{
    g_CopyBoneData(dest, cachedPose, boneCount);
}

bool patchUpdateSourceBones(uintptr_t hookPoint)
{
    uint8_t* f = (uint8_t*)hookPoint;
    uint8_t* t = (uint8_t*)VirtualAlloc(NULL, 4096, MEM_COMMIT | MEM_RESERVE,
        PAGE_EXECUTE_READWRITE);
    if (!t)
        return false;

    uint8_t* p = t;
    uint64_t ret = hookPoint + 0x16D, func = (uint64_t)CopyBoneData;
    uint8_t stub[] = { 0x41, 0x57, 0x49, 0x89, 0xE7, 0x48, 0x83, 0xE4, 0xF0, 0x48, 0x83, 0xEC, 0x20, 0x48, 0x8B, 0x4D, 0xF7, 0x48, 0x89, 0xDA, 0x45, 0x89, 0xE8, 0x48, 0xB8 };
    memcpy(p, stub, sizeof(stub));
    p += sizeof(stub);
    memcpy(p, &func, 8);
    p += 8;
    uint8_t tail[] = { 0xFF, 0xD0, 0x4C, 0x89, 0xFC, 0x41, 0x5F, 0x48, 0xB8 };
    memcpy(p, tail, sizeof(tail));
    p += sizeof(tail);
    memcpy(p, &ret, 8);
    p += 8;
    *p++ = 0xFF;
    *p++ = 0xE0;

    uint64_t tramAddr = (uint64_t)t;
    uint8_t p1[] = {0x41, 0x6B, 0xC5, 0x04, 0x90};
    uint8_t p2[99];
    memset(p2, 0x90, sizeof(p2));
    p2[0] = 0x48;
    p2[1] = 0xB8;
    memcpy(&p2[2], &tramAddr, 8);
    p2[10] = 0xFF;
    p2[11] = 0xE0;

    DWORD old;
    if (!VirtualProtect((void *)hookPoint, 0x16F, PAGE_EXECUTE_READWRITE, &old))
        goto fail;
    memcpy(f + 0x6A, p1, 5);
    memcpy(f + 0xAC, p1, 5);
    memcpy(f + 0x10A, p2, 99);
    if (!VirtualProtect((void *)hookPoint, 0x16F, old, &old))
        goto fail;
    FlushInstructionCache(GetCurrentProcess(), (void *)hookPoint, 0x16F);
    return true;

fail:
    VirtualFree(t, 0, MEM_RELEASE);
    return false;
}

void SkinPatch_SetSwitchDqsOn(bool on) {
    g_SwitchDqsOn = on;
    if (g_SwitchDqsOn) {
        g_CopyBoneData = cpuHasAvx2Fma ? CopyBoneMatrixAndDecomposeAVX2FMA : CopyBoneMatrixAndDecomposeSSE4;
    } else {
        g_CopyBoneData = CopyBoneMatrixSSE4;
    }
}