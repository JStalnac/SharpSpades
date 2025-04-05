#include "types.h"

struct map;

int validate_hit(vec3f shooter,
             vec3f orientation,
             vec3f other,
             float tolerance);
long can_see(struct map *, float x0, float y0, float z0, float x1, float y1, float z1);
long cast_ray(struct map *, vec3f from, vec3f direction, float length, vec3l *hit);
