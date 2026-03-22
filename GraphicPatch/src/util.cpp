#include "pch.h"
#include "util.h"

bool DetectAvx2Fma()
{
    int info[4];
    __cpuid(info, 1);
    bool fma = (info[2] & (1 << 12)) != 0;
    __cpuidex(info, 7, 0);
    bool avx2 = (info[1] & (1 << 5)) != 0;
    return avx2 && fma;
}

bool cpuHasAvx2Fma = DetectAvx2Fma();
XXHash3_64_Func g_XXHash3_64 = cpuHasAvx2Fma ? XXHash3_64_AVX2FMA : XXHash3_64_SSE4;

extern "C" __declspec(dllexport) int GetPhysicalProcessorCount() {
    DWORD len = 0;
    GetLogicalProcessorInformation(nullptr, &len);
    if (len == 0) {
        SYSTEM_INFO si;
        GetSystemInfo(&si);
        return (int)si.dwNumberOfProcessors;
    }
    DWORD count = len / sizeof(SYSTEM_LOGICAL_PROCESSOR_INFORMATION);
    auto buffer = std::make_unique<SYSTEM_LOGICAL_PROCESSOR_INFORMATION[]>(count);
    if (!GetLogicalProcessorInformation(buffer.get(), &len)) {
        SYSTEM_INFO si;
        GetSystemInfo(&si);
        return (int)si.dwNumberOfProcessors;
    }
    int physicalCores = 0;
    for (DWORD i = 0; i < count; i++) {
        if (buffer[i].Relationship == RelationProcessorCore) {
            physicalCores++;
        }
    }
    return physicalCores > 0 ? physicalCores : 1;
}

int GetLogicalProcessorCount() {
    SYSTEM_INFO si;
    GetSystemInfo(&si);
    return (int)si.dwNumberOfProcessors;
}

uint64_t XXHash3_64(const uint8_t* data, unsigned int len, uint64_t seed)
{
    return g_XXHash3_64(data, len, seed);
}
