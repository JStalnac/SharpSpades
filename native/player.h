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

struct player
{
	enum tool item;
	char movForward;
	char movBackwards;
	char movLeft;
	char movRight;
	char jumping;
	char crouching;
	char sneaking;
	char sprinting;
	char primary_fire;
	char secondary_fire;

	struct
	{
		vec3f pos;
		vec3f eyePos;
		vec3f vel;
		vec3f strafeOrientation;
		vec3f heightOrientation;
		vec3f forwardOrientation;
		vec3f previousOrientation;
	} m;
	char   airborne;
	char   wade;
	float  lastclimb;
};

struct player *player_create();
void player_destroy(struct player *);
void player_set_orientation(struct player *, vec3f orientation);
int player_try_uncrouch(struct map *, struct player *);
long move_player(struct map *, struct player *, float delta, float time);
