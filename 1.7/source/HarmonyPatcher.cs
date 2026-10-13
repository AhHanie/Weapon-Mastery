using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;


namespace SK_WeaponMastery
{

    // Responsible for patching pre/post fix methods into the game's
    // original methods
    public static class HarmonyPatcher
    {
        public static Harmony instance;

        // Harmony 3 infix patches. Instead of running after the whole verb
        // method and re-deriving vanilla's conditions, they wrap the single
        // Pawn_SkillTracker.Learn call vanilla makes when a pawn earns
        // Shooting/Melee XP. The mastery XP then follows vanilla's rules
        // (target conditions, colony mechs, XP amount) automatically.
        [HarmonyPatch(typeof(Verb_Shoot), nameof(Verb_Shoot.WarmupComplete))]
        public static class ShootExperienceInfix
        {
            [HarmonyInfix(typeof(Pawn_SkillTracker), nameof(Pawn_SkillTracker.Learn))]
            public static void Postfix([HarmonyOuter] Verb_Shoot __instance, float xp)
            {
                Core.AddWeaponExp(__instance.CasterPawn, (int)xp);
            }
        }

        [HarmonyPatch(typeof(Verb_MeleeAttack), "TryCastShot")]
        public static class MeleeExperienceInfix
        {
            [HarmonyInfix(typeof(Pawn_SkillTracker), nameof(Pawn_SkillTracker.Learn))]
            public static void Postfix([HarmonyOuter] Verb_MeleeAttack __instance, float xp)
            {
                Core.AddWeaponExp(__instance.CasterPawn, (int)xp);
            }
        }

        public static void PatchVanillaMethods()
        {
            // Mastery XP when a pawn shoots or hits. Fall back to plain
            // postfixes if the infix targets can't be resolved (for example
            // another mod's transpiler removed vanilla's Learn call)
            if (!TryPatchClass(typeof(ShootExperienceInfix)))
                PatchPostfix(typeof(Verb_Shoot), "WarmupComplete", "OnPawnShoot");
            if (!TryPatchClass(typeof(MeleeExperienceInfix)))
                PatchPostfix(typeof(Verb_MeleeAttack), "TryCastShot", "OnPawnMelee");

            PatchPostfix(typeof(Mod), "WriteSettings", "OnModWriteSettings");

            if (ModSettings.useGeneralMasterySystem)
                PatchPostfix(typeof(RaceProperties), "SpecialDisplayStats", "AddMasteryDescriptionToDrawStats");

            // Patch these when mastery on outsider pawns is enabled in config
            if (ModSettings.masteryOnOutsidePawns)
            {
                PatchPostfix(typeof(IncidentWorker_Raid), "TryGenerateRaidInfo", "OnRaid");
                PatchPostfix(typeof(IncidentWorker_NeutralGroup), "SpawnPawns", "OnNeutralPawnSpawn");
            }

            if (ModSettings.displayExperience)
                PatchPostfix(typeof(Dialog_InfoCard), "Setup", "OnInfoWindowSetup");

            PatchPostfix(typeof(Pawn), "GetGizmos", "DisplayAdditionalPawnGizmos");
        }

        // Apply a [HarmonyPatch] class. Harmony 3 patching is transactional so
        // a failure leaves the target untouched
        private static bool TryPatchClass(Type patchClass)
        {
            try
            {
                instance.CreateClassProcessor(patchClass).Patch();
                return true;
            }
            catch (Exception e)
            {
                Log.Warning($"[Weapon Mastery] Could not apply {patchClass.Name}, using a fallback patch instead: {e.Message}");
                return false;
            }
        }

        // Postfix a game method with one of the Core methods. A missing
        // target is logged and skipped so the rest of the mod still loads
        private static void PatchPostfix(Type type, string methodName, string coreMethodName)
        {
            MethodInfo target = AccessTools.Method(type, methodName);
            MethodInfo postfix = AccessTools.Method(typeof(Core), coreMethodName);
            if (target == null || postfix == null)
            {
                Log.Warning($"[Weapon Mastery] Skipped patch, could not find {type.Name}.{methodName} or Core.{coreMethodName}.");
                return;
            }
            try
            {
                instance.Patch(target, null, new HarmonyMethod(postfix));
            }
            catch (Exception e)
            {
                Log.Error($"[Weapon Mastery] Failed to patch {type.Name}.{methodName}: {e}");
            }
        }

        public static void SetInstance(Harmony instance)
        {
            HarmonyPatcher.instance = instance;
        }
    }
}
