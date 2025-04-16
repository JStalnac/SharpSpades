/*
 * Copyright (C) 2011-2012  Mathias Kearlev (original)
 * Copyright (C) 2021-2025  DarkNeutrino
 * Copyright (C) 2025  JStalnac
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

#include <math.h>
#include <stdlib.h>

#include "map.h"

#include "grenade.h"

struct grenade *
grenade_create(vec3f position, vec3f velocity)
{
	struct grenade *g;

	if (!(g = malloc(sizeof(*g))))
		return NULL;
	g->pos = position;
	g->vel = velocity;
	return g;
}

void
grenade_destroy(struct grenade *g)
{
	if (!g)
		return;
	free(g);
}

static inline long clipworld(struct map *map, long x, long y, long z)
{
	int sz;

	if (x < 0 || x >= 512 || y < 0 || y >= 512)
		return 0;
	if (z < 0)
		return 0;
	sz = (int) z;
	if (sz == 63)
		sz = 62;
	else if (sz >= 63)
		return 1;
	else if (sz < 0)
		return 0;
	return map_is_solid(map, (int) x, (int) y, sz);
}

// returns 1 if there was a collision, 2 if sound should be played
int move_grenade(struct map *map, struct grenade *g, float delta)
{
	vec3f fpos = g->pos; // old position
	// do vel & gravity (friction is negligible)
	float f = delta * 32;
	g->vel.z += delta;
	g->pos.x +=
	g->vel.x * f;
	g->pos.y +=
	g->vel.y * f;
	g->pos.z +=
	g->vel.z * f;
	// do rotation
	// FIX ME: Loses orientation after 45 degree bounce off wall
	//  if(g->v.x > 0.1f || g->v.x < -0.1f || g->v.y > 0.1f || g->v.y < -0.1f)
	//  {
	//  f *= -0.5;
	// }
	// make it bounce (accurate)
	vec3l lp;
	lp.x = (long) floor(g->pos.x);
	lp.y = (long) floor(g->pos.y);
	lp.z = (long) floor(g->pos.z);

	int ret = 0;

	if (clipworld(map, lp.x, lp.y, lp.z)) // hit a wall
	{
#define BOUNCE_SOUND_THRESHOLD 0.1f

		ret = 1;
		if (fabs(g->vel.x) > BOUNCE_SOUND_THRESHOLD ||
		    fabs(g->vel.y) > BOUNCE_SOUND_THRESHOLD ||
		    fabs(g->vel.z) > BOUNCE_SOUND_THRESHOLD)
			ret = 2; // play sound

		vec3l lp2;
		lp2.x = (long) floor(fpos.x);
		lp2.y = (long) floor(fpos.y);
		lp2.z = (long) floor(fpos.z);
		if (lp.z != lp2.z && ((lp.x == lp2.x && lp.y == lp2.y) || !clipworld(map, lp.x, lp.y, lp2.z)))
			g->vel.z = -g->vel.z;
		else if (lp.x != lp2.x && ((lp.y == lp2.y && lp.z == lp2.z) || !clipworld(map, lp2.x, lp.y, lp.z)))
			g->vel.x = -g->vel.x;
		else if (lp.y != lp2.y && ((lp.x == lp2.x && lp.z == lp2.z) || !clipworld(map, lp.x, lp2.y, lp.z)))
			g->vel.y = -g->vel.y;
		g->pos = fpos; // set back to old position
		g->vel.x *= 0.36f;
		g->vel.y *= 0.36f;
		g->vel.z *= 0.36f;
	}
	return ret;
}
