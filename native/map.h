#include "types.h"

#define MAP_X 512
#define MAP_Y 512
#define MAP_Z 64
#define COLOR_MASK 0xFF000000
#define DEFAULT_COLOR 0xFF674028
#define AIR (0x00FFFFFF & DEFAULT_COLOR)

/*
 * The block type is essentially
 * union {
 * 	struct {
 * 		uint8_t b;
 * 		uint8_t g;
 * 		uint8_t r;
 * 		uint8_t data; 0xFF for solid, 0x00 for air
 * 	};
 * 	uint32_t raw;
 * }
 */
typedef uint32_t block;

struct map {
	block blocks[MAP_X][MAP_Y][MAP_Z];
};

struct map *map_create();
void map_destroy(struct map *);
void map_load(struct map *, const uint8_t *v, int len);
void map_write(const struct map *, char *filename);

void map_set(struct map *, uint16_t x, uint16_t y, uint16_t z, block b);
block map_get(const struct map *, uint16_t x, uint16_t y, uint16_t z);
int map_is_solid(const struct map *, uint16_t x, uint16_t y, uint16_t z);
int map_is_surface(const struct map *, uint16_t x, uint16_t y, uint16_t z);

int map_block_line(const vec3i* v1, const vec3i* v2, vec3i* result);
