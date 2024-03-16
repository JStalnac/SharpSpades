namespace SharpSpades.Api.Weapons;

public interface IWeapon
{
    bool Shooting { get; }
    bool Reloading { get; }
    int Stock { get; }
    int Ammo { get; }
    float Rate { get; }

    void Restock();
    bool SetShooting(bool shooting);
    int GetDamage(HitType hit);
}