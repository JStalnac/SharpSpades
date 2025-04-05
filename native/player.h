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
