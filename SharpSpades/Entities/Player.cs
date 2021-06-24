using Microsoft.Extensions.Logging;
using SharpSpades.Native;
using SharpSpades.Net;
using System;
using System.Numerics;
using System.Threading.Tasks;

namespace SharpSpades.Entities
{
    public unsafe class Player : Entity
    {
        public string Name => Client.Name;
        public Client Client { get; set; }
        public bool IsInWater => NativePlayer->Wade != 0 ? true : false;
        public bool IsAirborne => NativePlayer->Airborne != 0 ? true : false;

        private NativePlayer* NativePlayer { get; }

        public InputState InputState
        {
            get
            {
                InputState state = (InputState)0;
                if (NativePlayer->Forward > 0)
                    state |= InputState.Up;
                if (NativePlayer->Backward > 0)
                    state |= InputState.Down;
                if (NativePlayer->Left > 0)
                    state |= InputState.Left;
                if (NativePlayer->Right > 0)
                    state |= InputState.Right;
                if (NativePlayer->Jump > 0)
                    state |= InputState.Jump;
                if (NativePlayer->Crouch > 0)
                    state |= InputState.Crouch;
                if (NativePlayer->Sneak > 0)
                    state |= InputState.Sneak;
                if (NativePlayer->Sprint > 0)
                    state |= InputState.Sprint;
                return state;
            }
            set
            {
                NativePlayer->Forward = value.HasFlag(InputState.Up) ? 1 : 0;
                NativePlayer->Backward = value.HasFlag(InputState.Down) ? 1 : 0;
                NativePlayer->Left = value.HasFlag(InputState.Left) ? 1 : 0;
                NativePlayer->Right = value.HasFlag(InputState.Right) ? 1 : 0;
                NativePlayer->Jump = value.HasFlag(InputState.Jump) ? 1 : 0;
                NativePlayer->Crouch = value.HasFlag(InputState.Crouch) ? 1 : 0;
                NativePlayer->Sneak = value.HasFlag(InputState.Sneak) ? 1 : 0;
                NativePlayer->Sprint = value.HasFlag(InputState.Sprint) ? 1 : 0;
            }
        }

        public override Vector3 Position
        {
            get => NativeVector.ToVector3(NativePlayer->Position);
            set
            {
                var position = NativeVector.FromVector3(value);
                NativePlayer->Position = position;
                NativePlayer->Position2 = position;
            }
        }

        public override Vector3 Rotation
        {
            get => NativeVector.ToVector3(NativePlayer->Orientation);
            set
            {
                var normalized = Vector3.Normalize(value);
                var o = new NativeVector()
                {
                    X = normalized.X,
                    Y = normalized.Y,
                    Z = normalized.Z
                };

                float f = MathF.Sqrt(o.X * o.X + o.Y * o.Y);

                var s = new NativeVector
                {
                    X = -o.Y / f,
                    Y = o.X / f
                };

                var h = new NativeVector
                {
                    X = -o.Z * s.Y,
                    Y = o.Z * s.X,
                    Z = o.X * s.Y - o.Y * s.X
                };

                NativePlayer->Orientation = o;
                NativePlayer->s = s;
                NativePlayer->h = h;
            }
        }

        public Vector3 Velocity
        {
            get => NativeVector.ToVector3(NativePlayer->Velocity);
            set => NativePlayer->Velocity = NativeVector.FromVector3(value);
        }

        private ILogger<Player> Logger { get; }

        internal Player(Client client)
        {
            Client = client;
            Logger = client.Server.GetLogger<Player>();
            NativePlayer = LibSharpSpades.create_player();
        }

        /// <summary>
        /// Frees the native player. VERY IMPORTANT!
        /// </summary>
        internal void Free()
        {
            LibSharpSpades.destroy_player(NativePlayer);
        }

        internal override Task UpdateAsync(float delta, float time)
        {
            LibSharpSpades.move_player(World.Map.NativeHandle, NativePlayer, delta, time);
            return base.UpdateAsync(delta, time);
        }
    }
}