#include "types.h"

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

#include "types.h"

enum
{
	ADDRESS_TYPE_ANY = 0,
	ADDRESS_TYPE_IPV4 = 1,
	ADDRESS_TYPE_IPV6 = 2
};

enum
{
	PACKET_FLAG_RELIABLE = (1 << 0),
	PACKET_FLAG_UNSEQUENCED = (1 << 1)
};

enum
{
	DISCONNECT_TYPE_NORMAL = 0,
	DISCONNECT_TYPE_TIMEOUT = 1
};

enum callback_result
{
	CALLBACK_RESULT_CONTINUE = 0,
	CALLBACK_RESULT_STOP = 1
};

typedef enum callback_result (*connect_callback)(uint32_t, uint32_t); /* client, ev->data */
typedef enum callback_result (*receive_callback)(uint32_t, uint8_t *, int32_t); /* client, buffer, length */
typedef enum callback_result (*disconnect_callback)(uint32_t, uint32_t); /* client, disconnect type */

struct host;

struct host *net_host_create_listener(int address_type,
                                      uint16_t port,
                                      size_t maxClients,
                                      size_t channels,
                                      uint32_t incoming_bandwidth,
                                      uint32_t outgoing_bandwidth);
void net_host_destroy(struct host *);

void net_host_set_connect_callback(struct host *, connect_callback);
void net_host_set_receive_callback(struct host *, receive_callback);
void net_host_set_disconnect_callback(struct host *, disconnect_callback);

int net_host_poll_events(struct host *, uint32_t service_timeout_ms);
