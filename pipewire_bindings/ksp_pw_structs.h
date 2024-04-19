#ifndef KSP_PW_STRUCTS_H
#define KSP_PW_STRUCTS_H

#include <stdint.h>
#include <stddef.h>
#include <stdbool.h>
#include <stdio.h>

typedef struct ksp_int24
{
    signed int data : 24;
} ksp_int24;

typedef struct waveFormatSubChunk
{
    uint16_t audioFormat;
    uint16_t channels;
    uint32_t sampleRate;
    uint32_t avgBytesPerSec;
    uint16_t blockAlign; //(Bits per sample * channels) / 8
    uint16_t bitsPerSample;
} waveFormatSubChunk;

typedef struct waveDataSubChunk
{
    uint32_t dataSize;
    uint8_t *data;
} waveDataSubChunk;

typedef struct waveFile
{
    char chunkId[5]; //Should always be RIFF
    uint32_t chunkSize;
    char format[5]; //Should always be WAVE
    struct waveFormatSubChunk formatChunk;
    struct waveDataSubChunk dataChunk;
} waveFile;

typedef struct waveFileLoadInfo
{
    bool mmapUsed;
    off_t mmapOffset;
    waveFile file;
} waveFileLoadInfo;

typedef enum AudioFormat
{
    Wave,
    MP3,
    Flac
} AudioFormat;

typedef struct pw_player_info
{
    float volume;
    char *fileName;
    bool playing;
    bool firstRun;
    AudioFormat format;
    int32_t fadeInMilliseconds;
    int32_t fadeOutMilliseconds;
    float speedFactor;
    struct waveData *data;
} pw_player_info;

typedef struct waveData
{
    struct pw_main_loop *loop;
    struct pw_stream *stream;

    size_t sampleIndex;
    struct waveFile file;

    pw_player_info *playerInfo;
} waveData;

#endif
