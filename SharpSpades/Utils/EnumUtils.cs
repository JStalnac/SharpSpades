namespace SharpSpades.Utils
{
    public static class EnumUtils
    {
        public static bool IsValid(this TeamType teamType)
        {
            int val = (int)teamType;
            return val == -1 || val == 0 || val == 1;
        }

        public static bool IsValid(this WeaponType weaponType)
        {
            int val = (int)weaponType;
            return val == 0 || val == 1 || val == 2;
        }

        public static bool IsValid(this Tool tool)
        {
            int val = (int)tool;
            return val == 0 || val == 1 || val == 2 || val == 3;
        }
    }
}