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

#include <assert.h>
#include <FLAC/stream_decoder.h>
#include <errno.h>
#include <libgen.h>
#include <math.h>
#include <pipewire-0.3/pipewire/loop.h>
#include <pipewire-0.3/pipewire/main-loop.h>
#include <pipewire-0.3/pipewire/stream.h>
#include <pipewire/context.h>
#include <pipewire/keys.h>
#include <pipewire/properties.h>
#include <pipewire/stream.h>
#include <pthread.h>
#include <signal.h>
#include <spa-0.2/spa/param/audio/raw.h>
#include <spa-0.2/spa/param/props.h>
#include <stdatomic.h>
#include <stdbool.h>
#include <stdint.h>
#include <stdio.h>
#include <string.h>
#include <time.h>

#include <spa/param/audio/format-utils.h>
#include <spa/param/props.h>

#include <pipewire/pipewire.h>
#include <wchar.h>

#include <fcntl.h>
#include <sys/mman.h>
#include <unistd.h>

#include "ksp_pw_structs.h"
#include "ksp_pw_process_funcs.h"
#include "ksp_pw_player_funcs.h"

void setVolume(const pw_player_info *info, float volume);
void setStreamVolume(struct pw_stream *stream, uint32_t channels, float volume);

waveFileLoadInfo ReadWave(const char *filePath)
{
    //size_t read = 0;
    FILE *file = fopen(filePath, "rb");
    struct waveFileLoadInfo output = {0};
    fread(output.file.chunkId, 1, 4, file);
    output.file.chunkId[4] = '\0';
    /*if (strcmp(output.chunkId, "fLaC") == 0) //Toto, I don't think we're in
    Redmond anymore
    {
        int8_t metadataBlockHeader;
        read += fread(&metadataBlockHeader, 1, 1, file);
        bool lastMetadataBlock = metadataBlockHeader & 0x70000000;
        uint8_t blockType = *(uint8_t*)&metadataBlockHeader & 0b01111111;
        int24 blockDataSize;
        read += fread(&blockDataSize, 3, 1, file);
        fputs("FLAC decoder support is not implemented!", stderr);
    }*/
    fread(&output.file.chunkSize, 4, 1, file);
    fread(output.file.format, 1, 4, file);
    output.file.format[4] = '\0';

    while (ftello(file) < output.file.chunkSize)
    {
        char blockId[5] = {0};
        fread(blockId, 1, 4, file);
        if (blockId[0] == '\0')
            break;
        if (strncmp(blockId, "fmt ", 4) == 0)
        {
            uint32_t fmtSize;
            fread(&fmtSize, 4, 1, file);
            fread(&output.file.formatChunk, sizeof(struct waveFormatSubChunk), 1, file);
            uint16_t dummy;
            size_t bytesRead = sizeof(struct waveFormatSubChunk);
            while (bytesRead < fmtSize)
            {
                fread(&dummy, 2, 1, file);
                bytesRead += 2;
            }
        }
        else if (strncmp(blockId, "data", 4) == 0)
        {
            uint32_t size;
            fread(&size, 4, 1, file);
            output.file.dataChunk.dataSize = size;

#ifdef USE_OLD_SOUND_FILE_READER
            output.dataChunk.data = malloc(size);
            if (output.dataChunk.data == NULL)
            {
                fprintf(stderr, "Could not allocate %u bytes for wave data!\n", output.dataChunk.dataSize);
                return output;
            }
            fread(output.dataChunk.data, 1, output.dataChunk.dataSize, file);
#else
            //Map the file into memory for faster reading and lower memory usage
            int fd = open(filePath, O_RDONLY);
            output.mmapUsed = true;
            output.mmapOffset = ftello(file);
            output.file.dataChunk.data = mmap(NULL, size, PROT_READ, MAP_PRIVATE, fd, 0) + output.mmapOffset;
            close(fd);
            fseeko(file, size, ftello(file));
            if (output.file.dataChunk.data == MAP_FAILED) 
            {
                fprintf(stderr, "Could not map file: %s\n", strerror(errno));
                output.file.dataChunk.data = NULL;
            }
#endif

        }
        else
        {
            uint32_t chunkSize;
            fread(&chunkSize, 4, 1, file);
            uint8_t dummy;
            for (uint32_t i = 0; i < chunkSize && !feof(file); i++)
            {
                fread(&dummy, 1, 1, file);
            }
        }
    }
    fclose(file);
    return output;
}

/* our data processing function is in general:
 *
 *  struct pw_buffer *b;
 *  b = pw_stream_dequeue_buffer(stream);
 *
 *  .. generate stuff in the buffer ...
 *
 *  pw_stream_queue_buffer(stream, b);
 */



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

    struct waveFileLoadInfo fileLoadInfo = ReadWave(filePath);
    if (fileLoadInfo.file.dataChunk.data == NULL) return;
    struct waveFile file = fileLoadInfo.file;
    struct waveData waveData = {0};
    //printf("%p\n", &waveData);
    info->data = &waveData;
    waveData.sampleIndex = 0;
    waveData.file = file;
    waveData.loop = pw_main_loop_new(NULL);
    waveData.playerInfo = info;
    pw_loop_add_signal(pw_main_loop_get_loop(waveData.loop), SIGINT, internalStopPlayer, &waveData);
    pw_loop_add_signal(pw_main_loop_get_loop(waveData.loop), SIGTERM, internalStopPlayer, &waveData);

    enum spa_audio_format format = SPA_AUDIO_FORMAT_UNKNOWN;

    struct pw_stream_events stream_events = {
        PW_VERSION_STREAM_EVENTS,
        .process = ksp_process_16,
        .command = NULL,
    };

    switch (file.formatChunk.bitsPerSample)
    {
        case 8:
            format = SPA_AUDIO_FORMAT_U8;
            stream_events.process = ksp_process_8;
            break;
        case 16:
            format = SPA_AUDIO_FORMAT_S16;
            stream_events.process = ksp_process_16;
            break;
        case 24:
            format = SPA_AUDIO_FORMAT_S24;
            stream_events.process = ksp_process_24;
            break;
        case 32:
            format = SPA_AUDIO_FORMAT_S32;
            stream_events.process = ksp_process_32;
            break;
        default:
            fprintf(stderr, "Unsupported audio bits per sample: %u", file.formatChunk.bitsPerSample);
    }

    //Samples for a time length- sampleRate * seconds

    /*uint32_t fadeInSamples = info->fadeInMilliseconds * file.formatChunk.sampleRate / 1000;
    //uint32_t fadeOutSamples = info->fadeOutMilliseconds * file.formatChunk.sampleRate / 1000;
    uint32_t bytesPerSample = file.formatChunk.bitsPerSample / 8; //Maybe replace with preprocessor constant later
    for (uint32_t i = 0; i < fadeInSamples && i < file.dataChunk.dataSize / bytesPerSample; i++)
    {
        for (int j = 0; j < bytesPerSample; j++)
        {
            file.dataChunk.data[(i * bytesPerSample) + j] *= (float)i / fadeInSamples;
        }
    }*/

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
                              PW_KEY_APP_ID, "com.calebmharper.ksp", PW_KEY_APP_NAME, "KarrotSoundProduction", NULL);
    /* Set stream target if given on command line */
    pw_properties_set(props, PW_KEY_MEDIA_FILENAME, filePath);
    waveData.stream = pw_stream_new_simple(pw_main_loop_get_loop(waveData.loop), basename(filePath), props,
                                           &stream_events, info);

    /* Make one parameter with the supported formats. The SPA_PARAM_EnumFormat
     * id means that this is a format enumeration (of 1 value). */
    uint32_t sampleRate = file.formatChunk.sampleRate * info->speedFactor;
    fprintf(stderr, "Using sample rate of %dHz(Actual rate is %dHz)\n", sampleRate, file.formatChunk.sampleRate);
    params[0] =
        spa_format_audio_raw_build(&b, SPA_PARAM_EnumFormat,
                                   &SPA_AUDIO_INFO_RAW_INIT(.format = format, .channels = file.formatChunk.channels,
                                                            .rate = sampleRate));

    clock_t end = clock();
    double elapsed = (end - start) / (double)CLOCKS_PER_SEC;
    printf("File load time: %.15gms\n", elapsed * 1000);

    /* Now connect this stream. We ask that our process function is
     * called in a realtime thread. */
    pw_stream_connect(waveData.stream, PW_DIRECTION_OUTPUT, PW_ID_ANY,
                      PW_STREAM_FLAG_AUTOCONNECT | PW_STREAM_FLAG_MAP_BUFFERS | PW_STREAM_FLAG_RT_PROCESS, params, 1);

    /* and wait while we let things run */
    pw_main_loop_run(waveData.loop);

    pw_stream_destroy(waveData.stream);
    pw_main_loop_destroy(waveData.loop);
    pw_deinit();

    if (fileLoadInfo.mmapUsed)
    {
        munmap(file.dataChunk.data - fileLoadInfo.mmapOffset, file.dataChunk.dataSize + fileLoadInfo.mmapOffset);
    }
    else
    {
        free(file.dataChunk.data);
    }
}
