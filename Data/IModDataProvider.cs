using System.Collections.Generic;
using BepInEx.Logging;

namespace ValheimGuide.DataGenerators
{
    public interface IModDataProvider
    {
        string ProviderId { get; }
        bool CanProvide(HashSet<string> installedMods);
        void Generate(string dataFolder, ManualLogSource log);
    }
}