#pragma once

#include <cstdint>

void Texture2DPatch_Init(uintptr_t allocateTextureDataAddr, uintptr_t mallocInternalAddr);

extern "C" __declspec(dllexport) bool Texture2DReInit(void* texture2d);
