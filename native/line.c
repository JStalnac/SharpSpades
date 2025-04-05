/*
    Copyright (c) Mathias Kaerlev 2011-2012.
    Modified by DarkNeutrino and CircumScriptor

    Cobe based upon pyspades in file world_c.cpp
    hugely modified to fit this project.
*/

#include <stdlib.h>

#include "line.h"

#define TMAX_ALT_VALUE  (0x3FFFFFFF / 1024)
#define MAX_LINE_LENGTH 50

/**
 * @brief Calculate block line
 *
 * @param v1 Start position
 * @param v2 End position
 * @param result Array of blocks positions
 * @return Number of block positions
 */
