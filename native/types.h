#ifndef TYPES_H
#define TYPES_H

#include <stdint.h>
#include <stdbool.h>

enum tool {
    TOOL_SPADE   = 0,
    TOOL_BLOCK   = 1,
    TOOL_GUN     = 2,
    TOOL_GRENADE = 3,
};

typedef struct
{
    int x;
    int y;
    int z;
} vec3i;

typedef struct
{
    float x;
    float y;
    float z;
} vec3f;

typedef struct
{
    long x;
    long y;
    long z;
} vec3l;

#endif
