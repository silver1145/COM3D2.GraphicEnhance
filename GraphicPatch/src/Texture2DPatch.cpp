#include "pch.h"
#include "Texture2DPatch.h"

struct Texture2D
{
    void* vtable;
    char pad0[72];
    void* m_TexData;
    char pad1[12];
    float m_TexelSizeX;
    float m_TexelSizeY;
    int m_MipCount;
    char pad2[64];
    int m_TextureState;
    int m_Width;
    int m_Height;
    int m_TextureFormat;
    int m_ImageSize;
    int m_ImageCount;
    int m_glWidth;
    int m_glHeight;
    int m_InitFlags;
    bool m_PowerOfTwo;
    bool m_TextureUploaded;
    bool m_UnscaledTextureUploaded;
    bool m_IsReadable;
    bool m_ReadAllowed;
    bool m_IsUnreloadable;
};

typedef void* (*AllocateTextureDataFunc)(void* mem, int allocType, int width, int height, int format, int imageSize, int imageCount, int mipCount, char clearMem, char allocData);
typedef void* (*MallocInternalFunc)(size_t size, int alignment, int allocType, int zero, void* memLabel, int callsite);

static AllocateTextureDataFunc g_AllocateTextureData = nullptr;
static MallocInternalFunc g_MallocInternal = nullptr;
static char s_dummyMemLabel[8] = {0};

void Texture2DPatch_Init(uintptr_t allocateTextureDataAddr, uintptr_t mallocInternalAddr) {
    g_AllocateTextureData = (AllocateTextureDataFunc)allocateTextureDataAddr;
    g_MallocInternal = (MallocInternalFunc)mallocInternalAddr;
}

bool Texture2DReInit(void* texture2dPtr)
{
    if (!texture2dPtr || !g_AllocateTextureData)
        return false;
    Texture2D* tex = (Texture2D*)texture2dPtr;
    if (tex->m_IsReadable && !tex->m_IsUnreloadable)
        return false;
    tex->m_IsReadable = true;
    tex->m_IsUnreloadable = false;
    if (tex->m_TexData)
        return false;
    int rawAlloc = *(int*)((char*)tex + 12) & 0x7FF;
    int allocType = (rawAlloc == 22) ? 22 : 19;
    void* mem = g_MallocInternal(80, 16, 19, 0, (void*)s_dummyMemLabel, 420);
    if (!mem)
        return false;
    memset(mem, 0, 80);
    g_AllocateTextureData(mem, allocType, tex->m_Width, tex->m_Height, tex->m_TextureFormat, tex->m_ImageSize, tex->m_ImageCount, tex->m_MipCount, 0, 1);
    tex->m_TexData = mem;
    return true;
}
