/*
 * Copyright (C) 2011-2012 Mathias Kearlev (original)
 * Copyright (C) 2021-2025 DarkNeutrino
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

#include <math.h>
#include <stddef.h>
#include <stdlib.h>

#include "map.h"

#include "player.h"

#define FALL_SLOW_DOWN 0.24f
#define FALL_DAMAGE_vel 0.58f
#define FALL_DAMAGE_SCALAR 4096
#define SQRT 0.70710678f

struct player *
player_create()
{
	struct player *p;

	if (!(p = malloc(sizeof(*p))))
		return NULL;
	return p;
}

void
player_destroy(struct player *p)
{
	if (p)
		free(p);
}

static inline int
clipbox(struct map *map, float x, float y, float z)
{
	int sz;

	if (x < 0 || x >= MAP_X || y < 0 || y >= MAP_Y)
		return 1;
	else if (z < 0)
		return 0;
	sz = (int) z;
	if (sz == 63)
		sz = 62;
	else if (sz >= 64)
		return 1;
	return map_is_solid(map, (int) x, (int) y, sz);
}

// original C code

static inline void
reposition_player(struct player *p, vec3f *pos, float time)
{
	p->m.eyePos = p->m.pos = *pos;
	float f = p->lastclimb - time;
	if (f > -0.25f)
		p->m.eyePos.z += (f + 0.25f) / 0.25f;
}

static inline void
set_orientation_vectors(vec3f *o, vec3f *s, vec3f *h)
{
	float f = sqrtf(o->x * o->x + o->y * o->y);
	if (f == 0.0f)
		f = 1.0f;
	s->x = -o->y / f;
	s->y = o->x / f;
	h->x = -o->z * s->y;
	h->y = o->z * s->x;
	h->z = o->x * s->y - o->y * s->x;
}

void
player_set_orientation(struct player *p, vec3f o)
{
	p->m.forwardOrientation = o;
	set_orientation_vectors(&o, &p->m.strafeOrientation, &p->m.heightOrientation);
}

int
player_try_uncrouch(struct map *map, struct player *p)
{
	float x1 = p->m.pos.x + 0.45f;
	float x2 = p->m.pos.x - 0.45f;
	float y1 = p->m.pos.y + 0.45f;
	float y2 = p->m.pos.y - 0.45f;
	float z1 = p->m.pos.z + 2.25f;
	float z2 = p->m.pos.z - 1.35f;

	// first check if player can lower feet (in midair)
	if (p->airborne && !(clipbox(map, x1, y1, z1) || clipbox(map, x1, y2, z1)
	                     || clipbox(map, x2, y1, z1) || clipbox(map, x2, y2, z1)))
		return (1);
	// then check if they can raise their head
	else if (!(clipbox(map, x1, y1, z2) || clipbox(map, x1, y2, z2) || clipbox(map, x2, y1, z2) ||
			   clipbox(map, x2, y2, z2)))
	{
		p->m.pos.z -= 0.9f;
		p->m.eyePos.z -= 0.9f;
		return (1);
	}
	return (0);
}

// player movement with autoclimb
void
boxclipmove(struct map *map, struct player *p, float time, float delta)
{
	float offset, m, f, nx, ny, nz, z;
	long  climb = 0;

	f  = delta * 32.f;
	nx = f * p->m.vel.x + p->m.pos.x;
	ny = f * p->m.vel.y + p->m.pos.y;

	if (p->crouching) {
		offset = 0.45f;
		m	  = 0.9f;
	} else {
		offset = 0.9f;
		m	  = 1.35f;
	}

	nz = p->m.pos.z + offset;

	if (p->m.vel.x < 0)
		f = -0.45f;
	else
		f = 0.45f;
	z = m;
	while (z >= -1.36f && !clipbox(map, nx + f, p->m.pos.y - 0.45f, nz + z) &&
		   !clipbox(map, nx + f, p->m.pos.y + 0.45f, nz + z))
		z -= 0.9f;
	if (z < -1.36f)
		p->m.pos.x = nx;
	else if (!p->crouching && p->m.forwardOrientation.z < 0.5f &&
			 !p->sprinting)
	{
		z = 0.35f;
		while (z >= -2.36f && !clipbox(map, nx + f, p->m.pos.y - 0.45f, nz + z) &&
			   !clipbox(map, nx + f, p->m.pos.y + 0.45f, nz + z))
			z -= 0.9f;
		if (z < -2.36f) {
			p->m.pos.x = nx;
			climb = 1;
		} else
			p->m.vel.x = 0;
	} else
		p->m.vel.x = 0;

	if (p->m.vel.y < 0)
		f = -0.45f;
	else
		f = 0.45f;
	z = m;
	while (z >= -1.36f && !clipbox(map, p->m.pos.x - 0.45f, ny + f, nz + z) &&
		   !clipbox(map, p->m.pos.x + 0.45f, ny + f, nz + z))
		z -= 0.9f;
	if (z < -1.36f)
		p->m.pos.y = ny;
	else if (!p->crouching && p->m.forwardOrientation.z < 0.5f &&
			 !p->sprinting && !climb)
	{
		z = 0.35f;
		while (z >= -2.36f && !clipbox(map, p->m.pos.x - 0.45f, ny + f, nz + z) &&
			   !clipbox(map, p->m.pos.x + 0.45f, ny + f, nz + z))
			z -= 0.9f;
		if (z < -2.36f) {
			p->m.pos.y = ny;
			climb = 1;
		} else
			p->m.vel.y = 0;
	} else if (!climb)
		p->m.vel.y = 0;

	if (climb) {
		p->m.vel.x *= 0.5f;
		p->m.vel.y *= 0.5f;
		p->lastclimb = time;
		nz--;
		m = -1.35f;
	} else {
		if (p->m.vel.z < 0)
			m = -m;
		nz += p->m.vel.z * delta * 32.f;
	}

	p->airborne = 1;

	if (clipbox(map,
				p->m.pos.x - 0.45f,
				p->m.pos.y - 0.45f,
				nz + m) ||
		clipbox(map,
				p->m.pos.x - 0.45f,
				p->m.pos.y + 0.45f,
				nz + m) ||
		clipbox(map,
				p->m.pos.x + 0.45f,
				p->m.pos.y - 0.45f,
				nz + m) ||
		clipbox(map,
				p->m.pos.x + 0.45f,
				p->m.pos.y + 0.45f,
				nz + m))
	{
		if (p->m.vel.z >= 0) {
			p->wade	 = p->m.pos.z > 61;
			p->airborne = 0;
		}
		p->m.vel.z = 0;
	} else
		p->m.pos.z = nz - offset;

	reposition_player(p, &p->m.pos, time);
}

long
move_player(struct map* map, struct player *p, float delta, float time)
{
	float f, f2;

	// move player and perform simple physics (gravity, momentum, friction)
	if (p->jumping) {
		p->jumping = 0;
		p->m.vel.z = -0.36f;
	}

	f = delta; // player acceleration scalar
	if (p->airborne)
		f *= 0.1f;
	else if (p->crouching)
		f *= 0.3f;
	else if ((p->secondary_fire && p->item == TOOL_GUN) ||
			 p->sneaking)
		f *= 0.5f;
	else if (p->sprinting)
		f *= 1.3f;

	if ((p->movForward || p->movBackwards) &&
		(p->movLeft || p->movRight))
		f *= SQRT; // if strafe + forward/backwards then limit diagonal velocity

	if (p->movForward) {
		p->m.vel.x += p->m.forwardOrientation.x * f;
		p->m.vel.y += p->m.forwardOrientation.y * f;
	} else if (p->movBackwards) {
		p->m.vel.x -= p->m.forwardOrientation.x * f;
		p->m.vel.y -= p->m.forwardOrientation.y * f;
	}
	if (p->movLeft) {
		p->m.vel.x -= p->m.strafeOrientation.x * f;
		p->m.vel.y -= p->m.strafeOrientation.y * f;
	} else if (p->movRight) {
		p->m.vel.x += p->m.strafeOrientation.x * f;
		p->m.vel.y += p->m.strafeOrientation.y * f;
	}

	f = delta + 1;
	p->m.vel.z += delta;
	p->m.vel.z /= f; // air friction
	if (p->wade)
		f = delta * 6.f + 1; // water friction
	else if (!p->airborne)
		f = delta * 4.f + 1; // ground friction
	p->m.vel.x /= f;
	p->m.vel.y /= f;
	f2 = p->m.vel.z;
	boxclipmove(map, p, time, delta);
	// hit ground... check if hurt
	if (!p->m.vel.z && (f2 > FALL_SLOW_DOWN)) {
		// slow down on landing
		p->m.vel.x *= 0.5f;
		p->m.vel.y *= 0.5f;

		// return fall damage
		if (f2 > FALL_DAMAGE_vel) {
			f2 -= FALL_DAMAGE_vel;
			return ((long) (f2 * f2 * FALL_DAMAGE_SCALAR));
		}

		return (-1); // no fall damage but play fall sound
	}

	return (0); // no fall damage
}
