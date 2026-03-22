#pragma once

extern "C" {
__declspec(dllexport) bool PatchInit();
}

struct SearchSignature {
    const BYTE* bytes;
    size_t size;
};
