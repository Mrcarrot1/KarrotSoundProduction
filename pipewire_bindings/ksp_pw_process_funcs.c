#include <pipewire-0.3/pipewire/pipewire.h>
#include <stdint.h>
#include <stdlib.h>

#include "ksp_pw_process_funcs.h"
#include "ksp_pw_structs.h"
#include "ksp_pw_player_funcs.h"

inline float get_volume(pw_player_info *info, waveData *data)
{
    float volume = 1;
    size_t sampleNumber = data->sampleIndex / (data->file.formatChunk.bitsPerSample / 8);
    size_t dataSizeSamples = data->file.dataChunk.dataSize / (data->file.formatChunk.bitsPerSample / 8);
    size_t fadeInSamples = info->fadeInMilliseconds * data->file.formatChunk.sampleRate / 1000 * data->file.formatChunk.channels;
    size_t fadeOutSamples = info->fadeOutMilliseconds * data->file.formatChunk.sampleRate / 1000 * data->file.formatChunk.channels;

    if (sampleNumber < fadeInSamples)
    {
        volume -= 1 - (float)sampleNumber / fadeInSamples;
    }
    if (dataSizeSamples - sampleNumber < fadeOutSamples)
    {
        volume -= 1 - (float)(dataSizeSamples - sampleNumber) / fadeOutSamples;
    }
    return volume;
}

void ksp_process_32(void *userdata)
{
    pw_player_info *info = userdata;
    struct waveData *data = info->data;
    /*if (info->firstRun)
    {
        setStreamVolume(data->stream, data->file.formatChunk.channels, 1);
        info->firstRun = false;
    }*/
    if (!data->playerInfo->playing)
        return;
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

    float volume = get_volume(info, data);

    for (i = 0; i < n_frames; i++)
    {
        for (c = 0; c < data->file.formatChunk.channels; c++)
        {
            val = *(int32_t *)(data->file.dataChunk.data + data->sampleIndex) * volume;
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

void ksp_process_24(void *userdata)
{
    pw_player_info *info = userdata;
    struct waveData *data = info->data;
    /*if (info->firstRun)
    {
        setStreamVolume(data->stream, data->file.formatChunk.channels, 1);
        info->firstRun = false;
    }*/
    if (!data->playerInfo->playing)
        return;
    struct pw_buffer *b;
    struct spa_buffer *buf;
    int i, c, n_frames, stride;
    uint8_t *dst;
    ksp_int24 val;

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

    float volume = get_volume(info, data);

    for (i = 0; i < n_frames; i++)
    {
        for (c = 0; c < data->file.formatChunk.channels; c++)
        {
            val = *(ksp_int24 *)(data->file.dataChunk.data + data->sampleIndex);
            val.data *= volume;
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

void ksp_process_16(void *userdata)
{
    pw_player_info *info = userdata;
    struct waveData *data = info->data;
    /*if (info->firstRun)
    {
        setStreamVolume(data->stream, data->file.formatChunk.channels, 1);
        info->firstRun = false;
    }*/
    if (!data->playerInfo->playing)
        return;
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

    float volume = get_volume(info, data);

    for (i = 0; i < n_frames; i++)
    {
        for (c = 0; c < data->file.formatChunk.channels; c++)
        {
            val = *(int16_t *)(data->file.dataChunk.data + data->sampleIndex) * volume;
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

void ksp_process_8(void *userdata)
{
    pw_player_info *info = userdata;
    struct waveData *data = info->data;
    /*if (info->firstRun)
    {
        setStreamVolume(data->stream, data->file.formatChunk.channels, 1);
        info->firstRun = false;
    }*/
    if (!data->playerInfo->playing)
        return;
    struct pw_buffer *b;
    struct spa_buffer *buf;
    int i, c, n_frames, stride;
    int8_t *dst;
    int8_t val;

    if ((b = pw_stream_dequeue_buffer(data->stream)) == NULL)
    {
        pw_log_warn("out of buffers: %m");
        return;
    }

    buf = b->buffer;
    if ((dst = buf->datas[0].data) == NULL)
        return;

    stride = 1 * data->file.formatChunk.channels;
    n_frames = buf->datas[0].maxsize / stride;

    float volume = get_volume(info, data);

    for (i = 0; i < n_frames; i++)
    {
        for (c = 0; c < data->file.formatChunk.channels; c++)
        {
            val = *(int8_t *)(data->file.dataChunk.data + data->sampleIndex) * volume;
            data->sampleIndex += 1;
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
