using System;
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
            if (tryGetOffHandEqMethod == null) return null;
            object[] methodParams = new object[] { pawn.equipment, null };
            bool result = (bool)tryGetOffHandEqMethod.Invoke(null, methodParams);
            return result ? (Thing)methodParams[1] : null;
        }

        // Returns false (and patches nothing) if Dual Wield doesn't expose
        // the methods we hook, so a changed Dual Wield version can't break
        // our own startup
        public static bool PatchMethods()
        {
            // DualWield Ext_Pawn TryStartOffHandAttack method
            MethodInfo tryStartOffHandAttackMethod = AccessTools.Method("DualWield.Ext_Verb:OffhandTryStartCastOn");
            // DualWield Stance_Warmup_DW Expire method
            MethodInfo expireMethod = AccessTools.Method("DualWield.Stances.Stance_Warmup_DW:Expire");
            MethodInfo getOffhandMethod = AccessTools.Method("DualWield.Ext_Pawn_EquipmentTracker:TryGetOffHandEquipment");
            if (tryStartOffHandAttackMethod == null || expireMethod == null || getOffhandMethod == null)
            {
                Log.Warning("[Weapon Mastery] Dual Wield is active but its methods could not be found, off-hand mastery is disabled.");
                return false;
            }

            HarmonyMethod tryCastPrefixMethod = new HarmonyMethod(typeof(DualWieldCompat).GetMethod("OffhandTryStartCastOnPrefix"));
            HarmonyMethod tryCastPostfixMethod = new HarmonyMethod(typeof(DualWieldCompat).GetMethod("OffhandTryStartCastOnPostfix"));
            HarmonyPatcher.instance.Patch(tryStartOffHandAttackMethod, tryCastPrefixMethod, tryCastPostfixMethod);

            HarmonyMethod expirePrefixMethod = new HarmonyMethod(typeof(DualWieldCompat).GetMethod("ExpirePrefix"));
            HarmonyMethod expirePostfixMethod = new HarmonyMethod(typeof(DualWieldCompat).GetMethod("ExpirePostfix"));
            HarmonyPatcher.instance.Patch(expireMethod, expirePrefixMethod, expirePostfixMethod);

            tryGetOffHandEqMethod = getOffhandMethod;
            return true;
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
            if (!ModsConfig.IsActive("Roolo.DualWield")) return;
            try
            {
                enabled = PatchMethods();
            }
            catch (Exception e)
            {
                Log.Error($"[Weapon Mastery] Failed to patch Dual Wield: {e}");
                enabled = false;
            }
        }
    }
}