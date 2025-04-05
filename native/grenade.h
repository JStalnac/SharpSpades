struct map;

struct grenade
{
	vec3f pos;
	vec3f vel;
};

struct grenade *grenade_create(vec3f position, vec3f velocity);
void grenade_destroy(struct grenade *);

int move_grenade(struct map *, struct grenade*, float delta);
