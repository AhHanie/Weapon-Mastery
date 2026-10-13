using System.Reflection;
using HarmonyLib;
using Verse;

namespace SK_WeaponMastery.Compat
{
    public static class DualWieldCompat
    {
        public static bool isCurrentAttackOffhand = false;
        public static bool enabled = false;
        private static MethodInfo tryGetOffHandEqMethod;

        public static Thing GetOffhandWeapon(Pawn pawn)
        {
            object[] methodParams = new object[] { pawn.equipment, null };
            bool result = (bool)tryGetOffHandEqMethod.Invoke(null, methodParams);
            return result ? (Thing)methodParams[1] : null;
        }

        public static void PatchMethods()
        {
            // Patch DualWield Ext_Pawn TryStartOffHandAttack method
            MethodInfo tryStartOffHandAttackMethod = AccessTools.Method("DualWield.Ext_Verb:OffhandTryStartCastOn");
            HarmonyMethod tryCastPrefixMethod = new HarmonyMethod(typeof(DualWieldCompat).GetMethod("OffhandTryStartCastOnPrefix"));
            HarmonyMethod tryCastPostfixMethod = new HarmonyMethod(typeof(DualWieldCompat).GetMethod("OffhandTryStartCastOnPostfix"));
            HarmonyPatcher.instance.Patch(tryStartOffHandAttackMethod, tryCastPrefixMethod, tryCastPostfixMethod);

            // Patch DualWield Stance_Warmup_DW Expire method
            MethodInfo expireMethod = AccessTools.Method("DualWield.Stances.Stance_Warmup_DW:Expire");
            HarmonyMethod expirePrefixMethod = new HarmonyMethod(typeof(DualWieldCompat).GetMethod("ExpirePrefix"));
            HarmonyMethod expirePostfixMethod = new HarmonyMethod(typeof(DualWieldCompat).GetMethod("ExpirePostfix"));
            HarmonyPatcher.instance.Patch(expireMethod, expirePrefixMethod, expirePostfixMethod);
        }

        public static void OffhandTryStartCastOnPostfix()
        {
            isCurrentAttackOffhand = false;
        }

        public static bool OffhandTryStartCastOnPrefix()
        {
            isCurrentAttackOffhand = true;
            return true;
        }

        public static bool ExpirePrefix()
        {
            isCurrentAttackOffhand = true;
            return true;
        }

        public static void ExpirePostfix()
        {
            isCurrentAttackOffhand = false;
        }

        public static void Init()
        {
            if (ModsConfig.IsActive("Roolo.DualWield"))
                enabled = true;
            if (!enabled) return;
            PatchMethods();
            tryGetOffHandEqMethod = AccessTools.Method("DualWield.Ext_Pawn_EquipmentTracker:TryGetOffHandEquipment");
        }
    }
}
