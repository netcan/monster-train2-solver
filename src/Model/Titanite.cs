using System;

namespace MonsterTrain2Poju.Model
{
    public static class TitaniteModel
    {
        public static int Adjust(int damage, bool piercing)
        {
            // Native Mathf.CeilToInt multiplies as a float, including its rounding
            // before the conversion for large integer damage values.
            return damage > 0 && !piercing ? Math.Max((int)Math.Ceiling(0.5f * damage), 1) : damage;
        }
    }
}
