using System;

namespace SK_WeaponMastery
{
    // Responsible for reading files from the filesystem
    public static class Files
    {
        public static string[] GetLinesFromTextFile(string path, bool relative)
        {
            try
            {
                if (!relative && System.IO.File.Exists(path))
                {
                    return System.IO.File.ReadAllLines(path);
                }
                else if (relative && System.IO.File.Exists(System.IO.Path.Combine(WeaponMasteryMod.GetRootDirectory(), path)))
                {
                    return System.IO.File.ReadAllLines(System.IO.Path.Combine(WeaponMasteryMod.GetRootDirectory(), path));
                }
                return null;
            }
            catch (Exception e)
            {
                return null;
            }
        }
    }
}
