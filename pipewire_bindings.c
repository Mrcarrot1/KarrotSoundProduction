/* KarrotSoundProduction PipeWire Interface
*  Adapted from PipeWire example audio-src.c
*  This Source Code Form is subject to the terms of the Mozilla Public
*  License, v. 2.0. If a copy of the MPL was not distributed with this
*  file, You can obtain one at http://mozilla.org/MPL/2.0/.
*
*  The copyright notice of the original file is below.
*/

/* PipeWire
 *
 * Copyright © 2018 Wim Taymans
 *
 * Permission is hereby granted, free of charge, to any person obtaining a
 * copy of this software and associated documentation files (the "Software"),
 * to deal in the Software without restriction, including without limitation
 * the rights to use, copy, modify, merge, publish, distribute, sublicense,
 * and/or sell copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice (including the next
 * paragraph) shall be included in all copies or substantial portions of the
 * Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.  IN NO EVENT SHALL
 * THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
 * DEALINGS IN THE SOFTWARE.
 */

/*
[title]
Audio source using \ref pw_stream "pw_stream".
[title]
*/

#include <errno.h>
#include <libgen.h>
#include <math.h>
#include <pipewire-0.3/pipewire/loop.h>
#include <pipewire-0.3/pipewire/main-loop.h>
#include <pipewire/context.h>
#include <pipewire/keys.h>
#include <pipewire/properties.h>
#include <pipewire/stream.h>
#include <pthread.h>
#include <signal.h>
#include <spa-0.2/spa/param/audio/raw.h>
#include <stdbool.h>
#include <stdint.h>
#include <stdio.h>
#include <string.h>
#include <time.h>
#include <FLAC/stream_decoder.h>

#include <spa/param/audio/format-utils.h>

#include <pipewire/pipewire.h>

//#define AUDIO_FILE "/home/mrcarrot/Music/Star Wars_The Ultimate Soundtrack Collection__John Williams 10 CD+1 DVD/CD 1 Episode I The Phantom Menace/score/01. John Williams - Star Wars Main Title and The Arrival at Naboo.wav"
//#define AUDIO_FILE "/home/mrcarrot/Music/Misc/01 - Never Gonna Give You Up.wav"
//#define AUDIO_FILE "/home/mrcarrot/Music/dpods/01 - September.wav"
//#define AUDIO_FILE "LengthTest.wav"
//#define AUDIO_FILE "32BitTest.wav"

typedef struct
{
    signed int data : 24;
} int24;

struct waveFormatSubChunk
{
    uint16_t audioFormat;
    uint16_t channels;
    uint32_t sampleRate;
    uint32_t avgBytesPerSec;
    uint16_t blockAlign; //(Bits per sample * channels) / 8
    uint16_t bitsPerSample;
};

struct waveDataSubChunk
{
    uint32_t dataSize;
    uint8_t *data;
};

typedef struct waveFile
{
    char chunkId[5]; // Should always be RIFF
    uint32_t chunkSize;
    char format[5]; // Should always be WAVE
    struct waveFormatSubChunk formatChunk;
    struct waveDataSubChunk dataChunk;
} waveFile;

typedef enum AudioFormat
{
    Wave,
    MP3,
    Flac
} AudioFormat;

typedef struct
{
    float volume;
    char *fileName;
    bool playing;
    AudioFormat format;
    int32_t fadeInMilliseconds;
    int32_t fadeOutMilliseconds;
    void *data;
} pw_player_info;

struct waveFile ReadWave(const char *filePath)
{
    size_t read = 0;
    FILE *file = fopen(filePath, "rb");
    struct waveFile output;
    read += fread(output.chunkId, 1, 4, file);
    output.chunkId[4] = '\0';
    /*if (strcmp(output.chunkId, "fLaC") == 0) //Toto, I don't think we're in Redmond anymore
    {   
        int8_t metadataBlockHeader;
        read += fread(&metadataBlockHeader, 1, 1, file);
        bool lastMetadataBlock = metadataBlockHeader & 0x70000000;
        uint8_t blockType = *(uint8_t*)&metadataBlockHeader & 0b01111111;
        int24 blockDataSize;
        read += fread(&blockDataSize, 3, 1, file);
        fputs("FLAC decoder support is not implemented!", stderr);
    }*/
    read += fread(&output.chunkSize, 4, 1, file);
    read += fread(output.format, 1, 4, file);
    output.format[4] = '\0';

    while (read < output.chunkSize)
    {
        char blockId[5] = {0};
        read += fread(blockId, 1, 4, file);
        if (blockId[0] == '\0')
            break;
        if (strcmp(blockId, "fmt ") == 0)
        {
            uint32_t fmtSize;
            read += fread(&fmtSize, 4, 1, file);
            read += fread(&output.formatChunk, sizeof(struct waveFormatSubChunk), 1, file);
            uint16_t dummy;
            size_t bytesRead = sizeof(struct waveFormatSubChunk);
            while (bytesRead < fmtSize)
            {
                read += fread(&dummy, 2, 1, file);
                bytesRead += 2;
            }
        }
        else if (strcmp(blockId, "data") == 0)
        {
            uint32_t size;
            read += fread(&size, 4, 1, file);
            output.dataChunk.dataSize = size;
            output.dataChunk.data = malloc(output.dataChunk.dataSize);
            if (output.dataChunk.data == NULL)
            {
                fprintf(stderr, "Could not allocate %u bytes for wave data!\n", output.dataChunk.dataSize);
                return output;
            }
            read += fread(output.dataChunk.data, 1, output.dataChunk.dataSize, file);
        }
        else
        {
            uint32_t chunkSize;
            read += fread(&chunkSize, 4, 1, file);
            uint8_t dummy;
            for (uint32_t i = 0; i < chunkSize; i++)
            {
                read += fread(&dummy, 1, 1, file);
            }
        }
    }
    return output;
}

struct data
{
    struct pw_main_loop *loop;
    struct pw_stream *stream;

    double accumulator;
};

struct waveData
{
    struct pw_main_loop *loop;
    struct pw_stream *stream;

    size_t sampleIndex;
    struct waveFile file;

    pw_player_info *playerInfo;
};

/* our data processing function is in general:
 *
 *  struct pw_buffer *b;
 *  b = pw_stream_dequeue_buffer(stream);
 *
 *  .. generate stuff in the buffer ...
 *
 *  pw_stream_queue_buffer(stream, b);
 */
static void on_process32(void *userdata)
{
    struct waveData *data = userdata;
    if (!data->playerInfo->playing) return;
    struct pw_buffer *b;
    struct spa_buffer *buf;
    int i, c, n_frames, stride;
    uint32_t *dst, val;

    if ((b = pw_stream_dequeue_buffer(data->stream)) == NULL)
    {
        pw_log_warn("out of buffers: %m");
        return;
    }

    buf = b->buffer;
    if ((dst = buf->datas[0].data) == NULL)
        return;

    stride = 4 * data->file.formatChunk.channels;
    n_frames = buf->datas[0].maxsize / stride;

    for (i = 0; i < n_frames; i++)
    {
        for (c = 0; c < data->file.formatChunk.channels; c++)
        {
            val = *(int32_t *)(data->file.dataChunk.data + data->sampleIndex) * data->playerInfo->volume;
            data->sampleIndex += 4;
            if (data->sampleIndex >= data->file.dataChunk.dataSize)
            {
                puts("Reached end of wave file.");
                pw_main_loop_quit(data->loop);
                return;
            }
            *dst++ = val;
        }
    }

    buf->datas[0].chunk->offset = 0;
    buf->datas[0].chunk->stride = stride;
    buf->datas[0].chunk->size = n_frames * stride;

    pw_stream_queue_buffer(data->stream, b);
}

static void on_process24(void *userdata)
{
    struct waveData *data = userdata;
    if (!data->playerInfo->playing) return;
    struct pw_buffer *b;
    struct spa_buffer *buf;
    int i, c, n_frames, stride;
    uint8_t *dst;
    int24 val;

    if ((b = pw_stream_dequeue_buffer(data->stream)) == NULL)
    {
        pw_log_warn("out of buffers: %m");
        return;
    }

    buf = b->buffer;
    if ((dst = buf->datas[0].data) == NULL)
        return;

    stride = 3 * data->file.formatChunk.channels;
    n_frames = buf->datas[0].maxsize / stride;

    for (i = 0; i < n_frames; i++)
    {
        for (c = 0; c < data->file.formatChunk.channels; c++)
        {
            val = *(int24 *)(data->file.dataChunk.data + data->sampleIndex);
            val.data *= data->playerInfo->volume;
            data->sampleIndex += 3;
            if (data->sampleIndex >= data->file.dataChunk.dataSize)
            {
                puts("Reached end of wave file.");
                pw_main_loop_quit(data->loop);
                return;
            }
            //*dst = val;
            memcpy(dst, &val, 3);
            dst += 3;
        }
    }

    buf->datas[0].chunk->offset = 0;
    buf->datas[0].chunk->stride = stride;
    buf->datas[0].chunk->size = n_frames * stride;

    pw_stream_queue_buffer(data->stream, b);
}

static void on_process16(void *userdata)
{
    struct waveData *data = userdata;
    if (!data->playerInfo->playing) return;
    struct pw_buffer *b;
    struct spa_buffer *buf;
    int i, c, n_frames, stride;
    int16_t *dst;
    int16_t val;

    if ((b = pw_stream_dequeue_buffer(data->stream)) == NULL)
    {
        pw_log_warn("out of buffers: %m");
        return;
    }

    buf = b->buffer;
    if ((dst = buf->datas[0].data) == NULL)
        return;

    stride = 2 * data->file.formatChunk.channels;
    n_frames = buf->datas[0].maxsize / stride;

    for (i = 0; i < n_frames; i++)
    {
        for (c = 0; c < data->file.formatChunk.channels; c++)
        {
            val = *(int16_t *)(data->file.dataChunk.data + data->sampleIndex) * data->playerInfo->volume;
            data->sampleIndex += 2;
            if (data->sampleIndex >= data->file.dataChunk.dataSize)
            {
                puts("Reached end of wave file.");
                pw_main_loop_quit(data->loop);
                return;
            }
            *dst++ = val;
        }
    }

    buf->datas[0].chunk->offset = 0;
    buf->datas[0].chunk->stride = stride;
    buf->datas[0].chunk->size = n_frames * stride;

    pw_stream_queue_buffer(data->stream, b);
}

static struct pw_stream_events stream_events = {
    PW_VERSION_STREAM_EVENTS,
    .process = on_process16,
};

void stopPlayer(pw_player_info *info)
{
    puts("Stopping player");
    struct data *data = info->data;
    pw_main_loop_quit(data->loop);
}

void internalStopPlayer(void *userdata, int signal)
{
    puts("Stopping player");
    struct data *data = userdata;
    pw_main_loop_quit(data->loop);
}

void startPlayer(pw_player_info *info, int argc, char **argv)
{
    puts("Native Pipewire backend: starting");
    info->format = Wave;
    clock_t start = clock();
    char *filePath = info->fileName;
    const struct spa_pod *params[2];
    uint8_t buffer[1024];
    struct pw_properties *props;
    struct spa_pod_builder b = SPA_POD_BUILDER_INIT(buffer, sizeof(buffer));

    pw_init(&argc, &argv);

    struct waveFile file = ReadWave(filePath);
    struct waveData waveData = {
        0,
    };
    info->data = &waveData;
    waveData.sampleIndex = 0;
    waveData.file = file;
    waveData.loop = pw_main_loop_new(NULL);
    waveData.playerInfo = info;
    pw_loop_add_signal(pw_main_loop_get_loop(waveData.loop), SIGINT, internalStopPlayer, &waveData);
    pw_loop_add_signal(pw_main_loop_get_loop(waveData.loop), SIGTERM, internalStopPlayer, &waveData);

    enum spa_audio_format format = SPA_AUDIO_FORMAT_UNKNOWN;

    switch (file.formatChunk.bitsPerSample)
    {
    case 16:
        format = SPA_AUDIO_FORMAT_S16;
        stream_events.process = on_process16;
        break;
    case 24:
        format = SPA_AUDIO_FORMAT_S24;
        stream_events.process = on_process24;
        break;
    case 32:
        format = SPA_AUDIO_FORMAT_S32;
        stream_events.process = on_process32;
    }

    /*struct data data = {
        0,
    };*/

    /* make a main loop. If you already have another main loop, you can add
     * the fd of this pipewire mainloop to it. */
    // data.loop = pw_main_loop_new(NULL);

    // pw_loop_add_signal(pw_main_loop_get_loop(data.loop), SIGINT, do_quit,
    // &data); pw_loop_add_signal(pw_main_loop_get_loop(data.loop), SIGTERM,
    // do_quit, &data);

    /* Create a simple stream, the simple stream manages the core and remote
     * objects for you if you don't need to deal with them.
     *
     * If you plan to autoconnect your stream, you need to provide at least
     * media, category and role properties.
     *
     * Pass your events and a user_data pointer as the last arguments. This
     * will inform you about the stream state. The most important event
     * you need to listen to is the process event where you need to produce
     * the data.
     */
    props = pw_properties_new(PW_KEY_MEDIA_TYPE, "Audio", PW_KEY_MEDIA_CATEGORY, "Playback", PW_KEY_MEDIA_ROLE, "Music",
                              NULL);
    /* Set stream target if given on command line */
    pw_properties_set(props, PW_KEY_MEDIA_FILENAME, filePath);
    waveData.stream =
        pw_stream_new_simple(pw_main_loop_get_loop(waveData.loop), basename(filePath), props, &stream_events, &waveData);

    /* Make one parameter with the supported formats. The SPA_PARAM_EnumFormat
     * id means that this is a format enumeration (of 1 value). */
    params[0] =
        spa_format_audio_raw_build(&b, SPA_PARAM_EnumFormat,
                                   &SPA_AUDIO_INFO_RAW_INIT(.format = format, 
                                        .channels = file.formatChunk.channels,
                                        .rate = file.formatChunk.sampleRate));

    clock_t end = clock();
    double elapsed = (end - start) / (double)CLOCKS_PER_SEC;
    printf("File load time: %.15gms\n", elapsed * 1000);
    printf("Data pointer: %p\n", info->data);

    /* Now connect this stream. We ask that our process function is
     * called in a realtime thread. */
    pw_stream_connect(waveData.stream, PW_DIRECTION_OUTPUT, PW_ID_ANY,
                      PW_STREAM_FLAG_AUTOCONNECT | PW_STREAM_FLAG_MAP_BUFFERS | PW_STREAM_FLAG_RT_PROCESS, params, 1);

    /* and wait while we let things run */
    pw_main_loop_run(waveData.loop);

    pw_stream_destroy(waveData.stream);
    pw_main_loop_destroy(waveData.loop);
    pw_deinit();

    free(file.dataChunk.data);
}