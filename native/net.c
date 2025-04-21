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

#include <limits.h>
#include <stddef.h>
#include <string.h>

#include <enet6/enet.h>

#include "types.h"
#include "net.h"

_Static_assert((int)ADDRESS_TYPE_ANY == (int)ENET_ADDRESS_TYPE_ANY, "ANY address types must match");
_Static_assert((int)ADDRESS_TYPE_IPV4 == (int)ENET_ADDRESS_TYPE_IPV4, "IPv4 address types must match");
_Static_assert((int)ADDRESS_TYPE_IPV6 == (int)ENET_ADDRESS_TYPE_IPV6, "IPv6 address types must match");

struct client
{
	uint32_t id; /* 0 means not connected */
	ENetPeer *peer;
};

struct host
{
	ENetHost *host;

	connect_callback connect_callback;
	receive_callback receive_callback;
	disconnect_callback disconnect_callback;

	/*
	 * TODO: Consider sorting this array so we can do binary search instead
	 * of linear search for finding client
	 */
	struct client *clients;
	size_t clients_len;
	uint32_t next_client_id;
};

struct host *
net_host_create_listener(int address_type, uint16_t port, size_t maxClients, size_t channels,
                uint32_t incoming_bandwidth, uint32_t outgoing_bandwidth)
{
	ENetAddress address;
	struct host *host;

	if (maxClients == 0)
		return NULL;

	if (!(host = malloc(sizeof(*host))))
		return NULL;
	memset(host, 0, sizeof(*host));

	host->connect_callback = NULL;
	host->receive_callback = NULL;
	host->disconnect_callback = NULL;

	if (!(host->clients = malloc(sizeof(*host->clients) * maxClients))) {
		free(host);
		return NULL;
	}
	memset(host->clients, 0, sizeof(*host->clients) * maxClients);
	host->clients_len = maxClients;

	host->next_client_id = 1;

	enet_address_build_loopback(&address, address_type);
	address.port = port;
	host->host = enet_host_create(address_type, &address,
	                              maxClients,
	                              channels,
	                              incoming_bandwidth,
	                              outgoing_bandwidth);
	if (!host->host)
	{
		free(host->clients);
		free(host);
		return NULL;
	}

	// Ace of Spades clients need this to be able to connect
	enet_host_compress_with_range_coder(host->host);

	return host;
}

void
net_host_destroy(struct host *host)
{
	if (!host)
		return;
	enet_host_destroy(host->host);
	free(host->clients);
	free(host);
}

void
net_host_set_connect_callback(struct host *host, connect_callback callback)
{
	if (!host)
		return;
	host->connect_callback = callback;
}

void
net_host_set_receive_callback(struct host *host, receive_callback callback)
{
	if (!host)
		return;
	host->receive_callback = callback;
}
void
net_host_set_disconnect_callback(struct host *host, disconnect_callback callback)
{
	if (!host)
		return;
	host->disconnect_callback = callback;
}


static int
net_host_handle_connect(struct host *host, ENetEvent *ev)
{
	struct client *c;
	size_t i;

	if (!host || !ev)
		return -1;

	for (i = 0; i < host->clients_len; i++) {
		c = &host->clients[i];
		if (c->id == 0)
			continue;

		// Free client slot
		c->id = host->next_client_id++;
		c->peer = ev->peer;
		c->peer->data = (void *)(uintptr_t)c->id;

		if (!host->connect_callback)
			return CALLBACK_RESULT_CONTINUE;

		return host->connect_callback(c->id, ev->data);
	}

	return -1;
}

static int
net_host_handle_receive(struct host *host, ENetEvent *ev)
{
	uint32_t client_id;
	int ret;

	if (!host || !ev)
		return -1;

	if (ev->packet->dataLength > INT_MAX)
		return -1;

	if (!host->receive_callback)
	{
		enet_packet_destroy(ev->packet);
		return CALLBACK_RESULT_CONTINUE;
	}

	client_id = (uint32_t)(uintptr_t)ev->peer->data;

	ret = host->receive_callback(client_id, ev->packet->data,
	                             (uint32_t)ev->packet->dataLength);

	enet_packet_destroy(ev->packet);

	return ret;
}

static int
net_host_handle_disconnect(struct host *host, ENetEvent *ev, uint32_t type)
{
	struct client *c;
	uint32_t client_id;
	size_t i;

	if (!host || !ev)
		return -1;

	client_id = (uint32_t)(uintptr_t)ev->peer->data;

	for (i = 0; i < host->clients_len; i++) {
		c = &host->clients[i];
		if (c->id != client_id)
			continue;

		memset(c, 0, sizeof(*c));
		c->id = 0;
		c->peer = NULL;

		if (!host->disconnect_callback)
			return CALLBACK_RESULT_CONTINUE;

		return host->disconnect_callback(client_id, type);
	}

	return -1;
}

int net_host_poll_events(struct host *host, uint32_t service_timeout_ms)
{
	ENetEvent ev;
	int ret;

	if (!host)
		return -1;

	ret = enet_host_service(host->host, &ev, service_timeout_ms);

	do
	{
		switch (ev.type)
		{
		case ENET_EVENT_TYPE_CONNECT:
			ret = net_host_handle_connect(host, &ev);
			break;
		case ENET_EVENT_TYPE_RECEIVE:
			ret = net_host_handle_receive(host, &ev);
			break;
		case ENET_EVENT_TYPE_DISCONNECT:
			ret = net_host_handle_disconnect(host, &ev,
			                DISCONNECT_TYPE_NORMAL);
			break;
		case ENET_EVENT_TYPE_DISCONNECT_TIMEOUT:
			ret = net_host_handle_disconnect(host, &ev,
			                DISCONNECT_TYPE_TIMEOUT);
			break;
		default:
			break;
		}

		if (ret < -1)
			return -1;
		if (ret == CALLBACK_RESULT_STOP)
			return 1;
	} while ((ret = enet_host_check_events(host->host, &ev)) > 0);

	return ret;
}
