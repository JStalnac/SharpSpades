/*
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

#include <stddef.h>
#include <string.h>

#include <enet6/enet.h>

#include "types.h"
#include "net.h"

struct host
{
	ENetHost *host;
};

struct host *
net_create_host()
{
	ENetAddress address;
	struct host *host;

	if (!(host = malloc(sizeof(*host))))
		return NULL;
	memset(host, 0, sizeof(*host));

	enet_address_build_any(&address, ENET_ADDRESS_TYPE_IPV6);
	address.port = 1234;
	host->host = enet_host_create(ENET_ADDRESS_TYPE_ANY, &address, 32, 2, 0, 0);
	return host;
}

void
net_host_destroy(struct host *host)
{
	if (!host)
		return;
	enet_host_destroy(host->host);
	free(host);
}
