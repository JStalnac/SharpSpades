using SharpSpades.Api;
using SharpSpades.Api.Entities;
using SharpSpades.Api.Net;
using System.Drawing;
using System.Numerics;

namespace SharpSpades.Entities
{
    public interface IPlayer : IEntity
    {
        string Name { get; }
        IClient Client { get; set; }

        byte Health { get; set; }

        TeamType Team { get; }
        WeaponType Weapon { get; }
        Tool Tool { get; set; }

        /// <summary>
        /// Color of the player's held block
        /// </summary>
        /// <value></value>
        Color Color { get; set; }

        bool PrimaryFire { get; set; }

        bool SecondaryFire { get; set; }
        
        bool IsInWater { get; }
        
        bool IsAirborne { get; }

        InputState InputState { get; set; }

        Vector3 EyePosition { get; }

        Vector3 Velocity { get; set; }

        Task KillAsync(byte killer, KillType type, byte respawnTime);

        /// <summary>
        /// Sets <see cref="Health"/> and sends a <see cref="SetHp"/> to the client.
        /// </summary>
        /// <param name="health">New health for the player.</param>
        /// <param name="source">The source location of the hit. If null, the damage will be applied as fall damage.</param>
        /// <returns></returns>
        Task SetHealthAsync(byte health, Vector3? source);

        /// <summary>
        /// Applies the specified amount of damage to the player. Negative values heal the player.
        /// </summary>
        /// <param name="amount">The amount of damage to apply. Negative values heal the player.</param>
        /// <remarks>Does not send a <see cref="SetHp"/> packet to the client.</remarks>
        void ApplyDamage(int amount);
    }
}