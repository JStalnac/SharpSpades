/*
 * Based on sample code from
 * http://silverspaceship.com/aosmap/aos_file_format.html
 *
 * Copyright (C) 2011 Silver Spaceship Software
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

#include <assert.h>
#include <stdlib.h>
#include <stdio.h>
#include <string.h>

#include "map.h"

struct map *
map_create()
{
	struct map *m;

	if (!(m = malloc(sizeof(*m))))
		return NULL;
	return m;
}

void
map_destroy(struct map *m)
{
	if (!m)
		return;
	free(m);
}

void
map_load(struct map *m, const uint8_t *v, int len)
{
	const uint8_t *base = v;
	int x, y, z;

	for (y = 0; y < MAP_X; y++) {
		for (x = 0; x < MAP_Y; x++) {
			z = 0;
			for(;;) {
				uint32_t *color;
				int i;
				int number_4byte_chunks = v[0];
				int top_color_start = v[1];
				int top_color_end   = v[2]; // inclusive
				int bottom_color_start;
				int bottom_color_end; // exclusive
				int len_top;
				int len_bottom;

				for(i = z; i < top_color_start; i++)
					map_set(m, x, y, i, AIR);

				color = (uint32_t *) (v+4);
				for(z = top_color_start; z <= top_color_end; z++)
					map_set(m, x, y, z, *color++);

				len_bottom = top_color_end - top_color_start + 1;

				// check for end of data marker
				if (number_4byte_chunks == 0) {
					// infer ACTUAL number of 4-byte chunks from the length of the color data
					v += 4 * (len_bottom + 1);
					break;
				}

				// infer the number of bottom colors in next span from chunk length
				len_top = (number_4byte_chunks-1) - len_bottom;

				// now skip the v pointer past the data to the beginning of the next span
				v += v[0]*4;

				bottom_color_end   = v[3]; // aka air start
				bottom_color_start = bottom_color_end - len_top;

				for(z = bottom_color_start; z < bottom_color_end; z++) {
					map_set(m, x, y, z, *color++);
				}
			}
		}
	}
	assert(v - base == len);
}

int
map_writer_init(struct map_writer *w)
{
	// 4 MB is the initial size for the buffer. Most maps are 2-3 MB encoded
	// and 4 MB (2^22 bytes) is the smallest power of two above that number.
	int cap = 4 * 1024 * 1024;

	memset(w, 0, sizeof(*w));

	if (!(w->buffer = malloc(sizeof(w->buffer) * cap))) {
		return -1;
	}
	w->capacity = cap;
	w->len = 0;
	return 0;
}

void
map_writer_deinit(struct map_writer *w)
{
	if (!w)
		return;
	free(w->buffer);
	memset(w, 0, sizeof(*w));
	w->buffer = NULL;
	w->capacity = 0;
	w->len = 0;
}

static void
map_writer_write_byte(struct map_writer *w, uint8_t b)
{
	int new_len;

	if (w->len >= w->capacity) {
		new_len = w->capacity;
		do {
			// Double the length until the contents fit
			new_len *= 2;

			// 64 MB is the maximum size of the encoded map at
			// 512x512x64 size
			assert(new_len <= 64 * 1024 * 1024);
			// TODO: Looping is unnecessary
		} while (new_len <= w->len);
		assert((w->buffer = realloc(w->buffer, new_len)) != NULL);
	}

	w->buffer[w->len++] = b;
}

static void
map_writer_write_color(struct map_writer *w, uint32_t color)
{
	// file format endianness is ARGB little endian, i.e. B,G,R,A
	map_writer_write_byte(w, (uint8_t) (color >>  0));
	map_writer_write_byte(w, (uint8_t) (color >>  8));
	map_writer_write_byte(w, (uint8_t) (color >> 16));
	map_writer_write_byte(w, (uint8_t) (color >> 24));
}

void
map_write(const struct map *m, struct map_writer *w)
{
	int i, j, k;

	for (j = 0; j < 512; j++) {
		for (i = 0; i < 512; i++) {
			/*int written_colors = 0;*/
			/*int backpatch_address = -1;*/
			/*int previous_bottom_colors = 0;*/
			/*int current_bottom_colors = 0;*/
			/*int middle_start = 0;*/

			k = 0;
			while (k < MAP_Z) {
				int z;

				int air_start;
				int top_colors_start;
				int top_colors_end; // exclusive
				int bottom_colors_start;
				int bottom_colors_end; // exclusive
				int top_colors_len;
				int bottom_colors_len;
				int colors;

				// find the air region
				air_start = k;
				while (k < MAP_Z && !map_is_solid(m, i, j, k))
					++k;

				// find the top region
				top_colors_start = k;
				while (k < MAP_Z && map_is_surface(m, i, j, k))
					++k;
				top_colors_end = k;

				// now skip past the solid voxels
				while (k < MAP_Z && map_is_solid(m, i, j, k) && !map_is_surface(m, i, j, k))
					++k;

				// at the end of the solid voxels, we have colored voxels.
				// in the "normal" case they're bottom colors; but it's
				// possible to have air-color-solid-color-solid-color-air,
				// which we encode as air-color-solid-0, 0-color-solid-air

				// so figure out if we have any bottom colors at this point
				bottom_colors_start = k;

				z = k;
				while (z < MAP_Z && map_is_surface(m, i, j, z))
					++z;

				if (z == MAP_Z || 0)
					; // in this case, the bottom colors of this span are empty, because we'll emit as top colors
				else {
					// otherwise, these are real bottom colors so we can write them
					while (map_is_surface(m, i, j, k))
						++k;
				}
				bottom_colors_end = k;

				// now we're ready to write a span
				top_colors_len = top_colors_end - top_colors_start;
				bottom_colors_len = bottom_colors_end - bottom_colors_start;

				colors = top_colors_len + bottom_colors_len;

				if (k == MAP_Z)
					map_writer_write_byte(w, 0); // last span
				else
					map_writer_write_byte(w, colors+1);
				map_writer_write_byte(w, top_colors_start);
				map_writer_write_byte(w, top_colors_end-1);
				map_writer_write_byte(w, air_start);

				for (z = 0; z < top_colors_len; z++)
					map_writer_write_color(w, map_get(m, i, j, top_colors_start + z));
				for (z = 0; z < bottom_colors_len; z++)
					map_writer_write_color(w, map_get(m, i, j, bottom_colors_start + z));
			}
		}
	}
}

void
map_set(struct map *m, uint16_t x, uint16_t y, uint16_t z, block b)
{
	m->blocks[x][y][z] = b;
}

block
map_get(const struct map *m, uint16_t x, uint16_t y, uint16_t z)
{
	return m->blocks[x][y][z];
}

int
map_is_solid(const struct map *m, uint16_t x, uint16_t y, uint16_t z)
{
	return (m->blocks[x][y][z] & COLOR_MASK) != 0;
}

int
map_is_surface(const struct map *m, uint16_t x, uint16_t y, uint16_t z)
{
	if (m->blocks[x][y][z] == AIR) return false;
	if (x     > 0     && m->blocks[x - 1][y][z] == AIR) return true;
	if (x + 1 < MAP_X && m->blocks[x + 1][y][z] == AIR) return true;
	if (y     > 0     && m->blocks[x][y - 1][z] == AIR) return true;
	if (y + 1 < MAP_Y && m->blocks[x][y + 1][z] == AIR) return true;
	if (z     > 0     && m->blocks[x][y][z - 1] == AIR) return true;
	if (z + 1 < MAP_Z && m->blocks[x][y][z + 1] == AIR) return true;
	return false;
}


/*
 *  Copyright (c) Mathias Kaerlev 2011-2012.
 *  Modified by DarkNeutrino and CircumScriptor
 *
 */

#define TMAX_ALT_VALUE  (0x3FFFFFFF / 1024)
#define MAX_LINE_LENGTH 50

int
block_line(const vec3i* v1, const vec3i* v2, vec3i* result)
{
	int count = 0;

	vec3i pos  = *v1;
	vec3i dist = {v2->x - v1->x, v2->y - v1->y, v2->z - v1->z};
	vec3i step;
	vec3i a;
	vec3i tmax;
	vec3i delta;

	step.x = dist.x < 0 ? -1 : 1;
	step.y = dist.y < 0 ? -1 : 1;
	step.z = dist.z < 0 ? -1 : 1;

	a.x = abs(dist.x);
	a.y = abs(dist.y);
	a.z = abs(dist.z);

	if (a.x >= a.y && a.x >= a.z) {
		tmax.x = 512;
		tmax.y = a.y != 0 ? a.x * 512 / a.y : TMAX_ALT_VALUE;
		tmax.z = a.z != 0 ? a.x * 512 / a.z : TMAX_ALT_VALUE;
	} else if (a.y >= a.z) {
		tmax.x = a.x != 0 ? a.y * 512 / a.x : TMAX_ALT_VALUE;
		tmax.y = 512;
		tmax.z = a.z != 0 ? a.y * 512 / a.z : TMAX_ALT_VALUE;
	} else {
		tmax.x = a.x != 0 ? a.z * 512 / a.x : TMAX_ALT_VALUE;
		tmax.y = a.y != 0 ? a.z * 512 / a.y : TMAX_ALT_VALUE;
		tmax.z = 512;
	}

	delta.x = tmax.x * 2;
	delta.y = tmax.y * 2;
	delta.z = tmax.z * 2;

	while (1) {
		result[count++] = pos;

		if (count >= MAX_LINE_LENGTH || (pos.x == v2->x && pos.y == v2->y && pos.z == v2->z)) {
			// reached limit or end
			break;
		}

		if (tmax.z <= tmax.x && tmax.z <= tmax.y) {
			pos.z += step.z;
			if (pos.z >= 64) {
				break;
			}
			tmax.z += delta.z;
		} else if (tmax.x < tmax.y) {
			pos.x += step.x;
			if (pos.x >= 512) {
				break;
			}
			tmax.x += delta.x;
		} else {
			pos.y += step.y;
			if (pos.y >= 512) {
				break;
			}
			tmax.y += delta.y;
		}
	}

	return count;
}
