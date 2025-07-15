using System.Globalization;
using RimWorld;
using Verse;

namespace SK_WeaponMastery
{
    public static class Utils
    {
        public static string Capitalize(string input)
        {
            TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
            return textInfo.ToTitleCase(input);
        }

        public static bool PawnIdeologyDespisesWeapon(Pawn pawn, Thing weapon)
        {
            return pawn.Ideo?.GetDispositionForWeapon(weapon.def) == IdeoWeaponDisposition.Despised;
        }
    }
}
