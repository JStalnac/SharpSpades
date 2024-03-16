using Microsoft.Extensions.Logging;
using SharpSpades.Api.Net;
using SharpSpades.Api.Net.Packets;
using SharpSpades.Native;
using SharpSpades.Net;
using System.Drawing;
using System.Numerics;

namespace SharpSpades.Entities
{
    public class Player : Entity, IPlayer
    {
        public string Name { get; }
        public TeamType Team { get; }
        public WeaponType Weapon { get; }

        public IClient Client { get; set; }

        private object _lock = new object();

        public byte Health
        {
            get
            {
                lock (_lock)
                    return Math.Min((byte)health, (byte)100);
            }
            set
            {
                // TODO: Error message
                if (value > 100)
                    throw new ArgumentOutOfRangeException(nameof(value));
                
                lock(_lock)
                    health = value;
            }
        }
        private int health = 100;

        public Tool Tool { get; set; }

        /// <summary>
        /// Color of the player's held block
        /// </summary>
        /// <value></value>
        public Color Color { get; set; }

        public bool PrimaryFire { get; set; }

        public bool SecondaryFire { get; set; }
        
        public unsafe bool IsInWater => NativePlayer->Wade != 0 ? true : false;
        
        public unsafe bool IsAirborne => NativePlayer->Airborne != 0 ? true : false;

        private unsafe NativePlayer* NativePlayer { get; }

        // Updated when properties are modified
        private volatile bool modified;

#region InputState
        public InputState InputState
        {
            get => inputState;
            set
            {
                inputState = value;
                modified = true;
            }
        }

        private InputState inputState;
#endregion

#region Position
        public override Vector3 Position
        {
            get => position;
            set
            {
                position = value;
                modified = true;
            }
        }

        private Vector3 position;

        public unsafe Vector3 EyePosition => NativeVector.ToVector3(NativePlayer->EyePosition);
#endregion

#region Rotation
        public override Vector3 Rotation
        {
            get => rotation;
            set
            {
                rotation = Vector3.Normalize(value);
                modified = true;
            }
        }

        private Vector3 rotation;
#endregion

#region Velocity
        public Vector3 Velocity
        {
            get => velocity;
            set
            {
                velocity = value;
                modified = true;
            }
        }

        private Vector3 velocity;
#endregion

        private ILogger<Player> Logger { get; }

        internal unsafe Player(IClient client)
        {
            Client = client;
            Name = client.Name!;
            Team = client.Team!.Value;
            Weapon = client.Weapon!.Value;

            Logger = client.Server.GetLogger<Player>();
            Logger.LogDebug("Allocating player");
            NativePlayer = LibSharpSpades.create_player();
        }

        public async Task KillAsync(byte killer, KillType type, byte respawnTime)
        {
            ((Client)Client).Player = null;
            World.RemoveEntity(this);
            Dispose();
            await Client.Server.BroadcastPacketAsync(new KillAction
            {
                PlayerId = Client.Id,
                KillerId = killer,
                KillType = type,
                RespawnTime = respawnTime
            });
        }

        /// <summary>
        /// Sets <see cref="Health"/> and sends a <see cref="SetHp"/> to the client.
        /// </summary>
        /// <param name="health">New health for the player.</param>
        /// <param name="source">The source location of the hit. If null, the damage will be applied as fall damage.</param>
        /// <returns></returns>
        public async Task SetHealthAsync(byte health, Vector3? source)
        {
            Health = health;

            DamageType type = source is not null 
                ? DamageType.Weapon
                : DamageType.Fall;
            
            await Client.SendPacketAsync(new SetHp
            {
                Health = health,
                Type = type,
                Source = source ?? new Vector3()
            });
        }

        /// <summary>
        /// Applies the specified amount of damage to the player. Negative values heal the player.
        /// </summary>
        /// <param name="amount">The amount of damage to apply. Negative values heal the player.</param>
        /// <remarks>Does not send a <see cref="SetHp"/> packet to the client.</remarks>
        public void ApplyDamage(int amount)
        {
            if (amount == 0)
                return;
            
            // No need to worry about overflows
            amount = Math.Clamp(amount, -100, 100);
            
            lock (_lock)
            {
                int h = health - amount;
                health = (byte)Math.Clamp(h, 0, 100);
            }
        }

        /// <summary>
        /// Frees the native player. VERY IMPORTANT!
        /// </summary>
        internal unsafe void Dispose()
        {
            Logger.LogDebug("Deallocating player");
            LibSharpSpades.destroy_player(NativePlayer);
        }

        public unsafe override Task UpdateAsync(float delta, float time)
        {
            if (modified)
            {
                // Set position
                var pos = NativeVector.FromVector3(position);
                NativePlayer->Position = pos;
                NativePlayer->EyePosition = pos;

                // The native code does this
                float f = NativePlayer->LastClimb - time;
                if (f > -0.25f)
                    NativePlayer->EyePosition.Z += (f + 0.25f) / 0.25f;

                // Set orientation
                var orientation = rotation;
                var o = new NativeVector()
                {
                    X = orientation.X,
                    Y = orientation.Y,
                    Z = orientation.Z
                };

                // Reuse
                f = MathF.Sqrt(o.X * o.X + o.Y * o.Y);

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
                NativePlayer->StrafeOrientation = s;
                NativePlayer->HeightOrientation = h;

                // Set velocity
                NativePlayer->Velocity = NativeVector.FromVector3(velocity);

                // Input stuff
                InputState input = InputState;
                NativePlayer->Forward = (byte)(input.HasFlag(InputState.Up) ? 1 : 0);
                NativePlayer->Backward = (byte)(input.HasFlag(InputState.Down) ? 1 : 0);
                NativePlayer->Left = (byte)(input.HasFlag(InputState.Left) ? 1 : 0);
                NativePlayer->Right = (byte)(input.HasFlag(InputState.Right) ? 1 : 0);
                NativePlayer->Jump = (byte)(input.HasFlag(InputState.Jump) ? 1 : 0);
                NativePlayer->Crouch = (byte)(input.HasFlag(InputState.Crouch) ? 1 : 0);
                NativePlayer->Sneak = (byte)(input.HasFlag(InputState.Sneak) ? 1 : 0);
                NativePlayer->Sprint = (byte)(input.HasFlag(InputState.Sprint) ? 1 : 0);
            }

            // Move player
            LibSharpSpades.move_player(World.Map.NativeHandle, NativePlayer, time, delta);

            // Update properties
            position = NativeVector.ToVector3(NativePlayer->Position);
            rotation = NativeVector.ToVector3(NativePlayer->Orientation);
            velocity = NativeVector.ToVector3(NativePlayer->Velocity);

            modified = false;

            // Only jump is changed in the native code
            inputState |= NativePlayer->Jump == 1 ? InputState.Jump : 0;

            return base.UpdateAsync(delta, time);
        }
    }
}