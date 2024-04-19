#include <assert.h>
#include <stdio.h>
#include <pipewire/pipewire.h>
#include <spa/param/props.h>


#include "ksp_pw_structs.h"

void setPaused(const pw_player_info *info, bool paused)
{
    struct waveData *data = (struct waveData*)info->data;
    pw_stream_set_active(data->stream, !paused);
}

void stopPlayer(pw_player_info *info)
{
    puts("Stopping player");
    struct waveData *data = (struct waveData*)info->data;
    info->playing = false;
    pw_main_loop_quit(data->loop);
}

void internalStopPlayer(void *userdata, int signal)
{
    puts("Stopping player");
    struct waveData *data = userdata;
    pw_main_loop_quit(data->loop);
}

void setStreamVolume(struct pw_stream *stream, uint32_t channels, float volume)
{
    float values[channels];
    for (uint32_t i = 0; i < channels; i++)
    {
        memcpy(&values[i], &volume, sizeof(float));
    }
    pw_stream_set_control(stream, SPA_PROP_channelVolumes, channels, values);
}

void setVolume(const pw_player_info *info, float volume)
{
    assert(info != NULL);
    struct waveData *data = info->data;
    
    assert(data != NULL);
    uint32_t channels = data->file.formatChunk.channels;
    setStreamVolume(data->stream, channels, volume);
}

float getVolume(const pw_player_info *info)
{
    const struct pw_stream_control *result = pw_stream_get_control(info->data->stream, SPA_PROP_channelVolumes);
    return result->values[0];
}