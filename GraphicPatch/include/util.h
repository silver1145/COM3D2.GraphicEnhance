#pragma once

bool DetectAvx2Fma();

extern bool cpuHasAvx2Fma;

typedef uint64_t(*XXHash3_64_Func)(const uint8_t *data, unsigned int len, uint64_t seed);

extern "C" __declspec(dllexport) void *ThreadPoolInit(int thread_count);
extern "C" __declspec(dllexport) void  ThreadPoolDeinit(void *pool);
extern "C" uint64_t XXHash3_64_SSE4(const uint8_t *data, unsigned int len, uint64_t seed);
extern "C" uint64_t XXHash3_64_AVX2FMA(const uint8_t *data, unsigned int len, uint64_t seed);
extern "C" __declspec(dllexport) int GetPhysicalProcessorCount();
extern "C" __declspec(dllexport) int GetLogicalProcessorCount();
extern "C" __declspec(dllexport) uint64_t XXHash3_64(const uint8_t *data, unsigned int len, uint64_t seed);
