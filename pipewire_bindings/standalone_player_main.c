#include <stdio.h>
#include "ksp_pw_player_main.h"
#include "ksp_pw_structs.h"

int main(int argc, char **argv)
{
    if (argc < 2)
    {
        puts("Please enter a file name!");
        return 1;
    }
    pw_player_info playerInfo = { .volume = 1, .fileName = argv[1], .playing = true };
    startPlayer(&playerInfo, argc, argv);
}
