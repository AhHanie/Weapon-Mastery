namespace SK_WeaponMastery
{
    // Responsible for updating ModSettings when variables in ModSettingsWindow
    // change through constant polling for changes
    class ModSettingsWindowThread
    {
        private const int THREAD_SLEEP_TIME_MS = 100;
        private int oldSelectedLevelValue;
        private float oldRangedStatOffset;
        private float oldMeleeStatOffset;

        public ModSettingsWindowThread(int oldSelectedLevelValue, float oldRangedStatOffset, float oldMeleeStatOffset)
        {
            this.oldSelectedLevelValue = oldSelectedLevelValue;
            this.oldRangedStatOffset = oldRangedStatOffset;
            this.oldMeleeStatOffset = oldMeleeStatOffset;
        }

        public void Run()
        {
            while (ModSettingsWindow.isOpen)
            {
                if (ModSettingsWindow.selectedLevelValue != oldSelectedLevelValue)
                {
                    ModSettings.experiencePerLevel[ModSettingsWindow.selectedLevelIndex] = ModSettingsWindow.selectedLevelValue;
                    oldSelectedLevelValue = ModSettingsWindow.selectedLevelValue;
                }
                if (ModSettingsWindow.selectedRangedMasteryStat != null && oldRangedStatOffset != ModSettingsWindow.rangedStatOffset)
                {
                    oldRangedStatOffset = ModSettingsWindow.rangedStatOffset;
                    ModSettingsWindow.selectedRangedMasteryStat.SetOffset((float)System.Math.Round(ModSettingsWindow.rangedStatOffset, 2));
                }
                if (ModSettingsWindow.selectedMeleeMasteryStat != null && oldMeleeStatOffset != ModSettingsWindow.meleeStatOffset)
                {
                    oldMeleeStatOffset = ModSettingsWindow.meleeStatOffset;
                    ModSettingsWindow.selectedMeleeMasteryStat.SetOffset((float)System.Math.Round(ModSettingsWindow.meleeStatOffset, 2));
                }
                if (!ModSettings.useGeneralMasterySystem && !ModSettings.useSpecificMasterySystem)
                    ModSettings.useSpecificMasterySystem = true;
                System.Threading.Thread.Sleep(THREAD_SLEEP_TIME_MS);
            }
        }
    }
}
