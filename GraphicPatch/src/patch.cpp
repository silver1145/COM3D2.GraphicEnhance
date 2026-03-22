#include "pch.h"
#include "patch.h"
#include "SkinPatch.h"
#include "Texture2DPatch.h"

bool PatchInit()
{
    const BYTE signatureInputSpec[] = { 0xC1, 0xE6, 0x10, 0x03, 0xF1, 0x48, 0x8B, 0x0D, 0x29, 0xAC, 0x14, 0x01, 0x4C, 0x8B, 0x41, 0x08 };
    const BYTE signatureGetMemExShader[] = { 0x41, 0x8B, 0x9C, 0xD7, 0x00, 0x1F, 0x0F, 0x01, 0x4D, 0x8B, 0xBC, 0xD7, 0x60, 0xB6, 0x39, 0x01 };
    const BYTE signatureUpdateSourceBones[] = { 0x48, 0x89, 0x5C, 0x24, 0x08, 0x48, 0x89, 0x74, 0x24, 0x10, 0x48, 0x89, 0x7C, 0x24, 0x18, 0x4C, 0x89, 0x64, 0x24, 0x20, 0x55, 0x41, 0x55, 0x41, 0x56, 0x48, 0x8D, 0x6C, 0x24, 0xB9, 0x48, 0x81, 0xEC, 0xB0 };
    const BYTE signatureAllocateTextureData[] = { 0x48, 0x89, 0x5C, 0x24, 0x08, 0x48, 0x89, 0x6C, 0x24, 0x10, 0x48, 0x89, 0x74, 0x24, 0x18, 0x57, 0x48, 0x83, 0xEC, 0x20, 0x48, 0x63, 0x5C, 0x24 };
    const BYTE signatureMallocInternal[] = { 0x48, 0x83, 0xEC, 0x38, 0x8B, 0x44, 0x24, 0x60, 0x4D, 0x8B, 0xD0, 0x44, 0x8B, 0xC2, 0x89, 0x44 };

    SearchSignature signatures[] = {
        {signatureInputSpec, sizeof(signatureInputSpec)},
        {signatureGetMemExShader, sizeof(signatureGetMemExShader)},
        {signatureUpdateSourceBones, sizeof(signatureUpdateSourceBones)},
        {signatureAllocateTextureData, sizeof(signatureAllocateTextureData)},
        {signatureMallocInternal, sizeof(signatureMallocInternal)},
    };

    uintptr_t baseAddr = (uintptr_t)GetModuleHandleA("COM3D2x64.exe");
    if (!baseAddr)
        return false;

    PIMAGE_DOS_HEADER dosHeader = (PIMAGE_DOS_HEADER)baseAddr;
    PIMAGE_NT_HEADERS ntHeaders = (PIMAGE_NT_HEADERS)(baseAddr + dosHeader->e_lfanew);
    PIMAGE_SECTION_HEADER section = IMAGE_FIRST_SECTION(ntHeaders);
    BYTE* results[sizeof(signatures) / sizeof(signatures[0])] = { nullptr };

    for (int i = 0; i < ntHeaders->FileHeader.NumberOfSections; i++)
    {
        if (memcmp(section[i].Name, ".text", 5) == 0)
        {
            BYTE* start = (BYTE*)(baseAddr + section[i].VirtualAddress);
            size_t sectionSize = section[i].Misc.VirtualSize;
            for (BYTE* p = start; p < start + sectionSize; ++p)
            {
                for (int sigIdx = 0; sigIdx < sizeof(signatures) / sizeof(signatures[0]); sigIdx++)
                {
                    if (results[sigIdx])
                        continue;
                    if (p + signatures[sigIdx].size > start + sectionSize)
                        continue;
                    if (memcmp(p, signatures[sigIdx].bytes, signatures[sigIdx].size) == 0)
                    {
                        results[sigIdx] = p;
                    }
                }
            }
            break;
        }
    }

    for (int i = 0; i < sizeof(results) / sizeof(results[0]); i++)
    {
        if (!results[i])
            return false;
    }

    if (!patchInputSpec((uintptr_t)results[0]))
        return false;

    if (!patchGetMemExShader((uintptr_t)results[1]))
        return false;

    if (!patchUpdateSourceBones((uintptr_t)results[2]))
        return false;

    Texture2DPatch_Init((uintptr_t)results[3], (uintptr_t)results[4]);

    // if (MH_Initialize() != MH_OK)
    //     return false;
    // if (MH_CreateHook(origin_skinning, &skinning, nullptr) != MH_OK)
    //     return false;
    // if (MH_EnableHook(origin_skinning) != MH_OK)
    //     return false;

    return true;
}

/* Unused
signature[] = { 0x48, 0x83, 0xEC, 0x28, 0x0F, 0xB6, 0x41, 0x58, 0x84, 0xC0, 0x74, 0x64 };
{
    { 0x1E,  0xE9, SkinMeshOptimized_1Bone_Pos },
    { 0x2C,  0xE9, SkinMeshOptimized_2Bone_Pos },
    { 0x3A,  0xE9, SkinMeshOptimized_4Bone_Pos },
    { 0x4F,  0xE9, SkinMeshOptimized_1Bone_PosNormal },
    { 0x5D,  0xE9, SkinMeshOptimized_2Bone_PosNormal },
    { 0x6B, 0xE9, SkinMeshOptimized_4Bone_PosNormal },
    { 0x7C, 0xE9, SkinMeshOptimized_1Bone_PosNormalTan },
    { 0x8A, 0xE9, SkinMeshOptimized_2Bone_PosNormalTan },
    { 0x94, 0xE8, SkinMeshOptimized_4Bone_PosNormalTan },
};
*/
