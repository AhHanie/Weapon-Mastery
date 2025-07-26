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

        public static void PatchVanillaMethods()
        {
            // Patch Verb_Shoot WarmupComplete method
            MethodInfo warmupCompleteMethod = AccessTools.Method(typeof(Verb_Shoot), "WarmupComplete");
            HarmonyMethod onPawnShootMethod = new HarmonyMethod(typeof(Core).GetMethod("OnPawnShoot"));
            instance.Patch(warmupCompleteMethod, null, onPawnShootMethod);

            // Patch Verb_MeleeAttack TryCastShot method
            MethodInfo tryCastShotMethod = AccessTools.Method(typeof(Verb_MeleeAttack), "TryCastShot");
            HarmonyMethod onPawnMelee = new HarmonyMethod(typeof(Core).GetMethod("OnPawnMelee"));
            instance.Patch(tryCastShotMethod, null, onPawnMelee);

            // Patch Mod WriteSettings method
            MethodInfo writeSettingsMethod = AccessTools.Method(typeof(Mod), "WriteSettings");
            HarmonyMethod onModWriteSettingsMethod = new HarmonyMethod(typeof(Core).GetMethod("OnModWriteSettings"));
            instance.Patch(writeSettingsMethod, null, onModWriteSettingsMethod);

            if (ModSettings.useGeneralMasterySystem)
            {
                // Patch RaceProperties SpecialDisplayStats method
                MethodInfo specialDisplayStatsMethod = AccessTools.Method(typeof(RaceProperties), "SpecialDisplayStats");
                HarmonyMethod addMasteryDescriptionToDrawStatsMethod = new HarmonyMethod(typeof(Core).GetMethod("AddMasteryDescriptionToDrawStats"));
                instance.Patch(specialDisplayStatsMethod, null, addMasteryDescriptionToDrawStatsMethod);
            }
            
            // Patch these when mastery on outsider pawns is enabled in config
            if (ModSettings.masteryOnOutsidePawns)
            {
                // Patch IncidentWorker_Raid TryGenerateRaidInfo method
                MethodInfo tryGenerateRaidMethod = AccessTools.Method(typeof(IncidentWorker_Raid), "TryGenerateRaidInfo");
                HarmonyMethod onRaidMethod = new HarmonyMethod(typeof(Core).GetMethod("OnRaid"));
                instance.Patch(tryGenerateRaidMethod, null, onRaidMethod);

                // Patch IncidentWorker_NeutralGroup SpawnPawns method
                MethodInfo spawnPawnsMethod = AccessTools.Method(typeof(IncidentWorker_NeutralGroup), "SpawnPawns");
                HarmonyMethod onNeutralPawnSpawnMethod = new HarmonyMethod(typeof(Core).GetMethod("OnNeutralPawnSpawn"));
                instance.Patch(spawnPawnsMethod, null, onNeutralPawnSpawnMethod);
            }

            if (ModSettings.displayExperience)
            {
                // Patch Dialog_InfoCard Setup method
                MethodInfo setupMethod = AccessTools.Method(typeof(Dialog_InfoCard), "Setup");
                HarmonyMethod onInfoWindowSetup = new HarmonyMethod(typeof(Core).GetMethod("OnInfoWindowSetup"));
                instance.Patch(setupMethod, null, onInfoWindowSetup);
            }
        }

        public static void SetInstance(Harmony instance)
        {
            HarmonyPatcher.instance = instance;
        }
    }
}
