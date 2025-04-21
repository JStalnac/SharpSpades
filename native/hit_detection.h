/*
 * Copyright (C) 2025 JStalnac
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

#include "types.h"

struct map;

int validate_hit(vec3f shooter,
             vec3f orientation,
             vec3f other,
             float tolerance);
long can_see(struct map *, float x0, float y0, float z0, float x1, float y1, float z1);
long cast_ray(struct map *, vec3f from, vec3f direction, float length, vec3l *hit);
