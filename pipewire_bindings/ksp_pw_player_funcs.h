#ifndef KSP_PW_PLAYER_FUNCS_H
#define KSP_PW_PLAYER_FUNCS_H

#include "ksp_pw_structs.h"

void setPaused(const pw_player_info *info, bool paused);

void stopPlayer(pw_player_info *info);

void internalStopPlayer(void *userdata, int signal);

void setStreamVolume(struct pw_stream *stream, uint32_t channels, float volume);

void setVolume(const pw_player_info *info, float volume);

float getVolume(const pw_player_info *info);

#endif