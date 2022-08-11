using Meep.Tech.XBam.Mods.Configuration;
using System.Collections.Generic;

namespace Meep.Tech.XBam.Mods {
  /// <summary>
  /// Helpers to get mods and resources from the universe
  /// </summary>
  public static class ModContextExtensions {

    /// <summary>
    /// Get the full mod data from the given universe.
    /// </summary>
    public static ModContext GetMods(this Universe universe)
      => universe.GetExtraContext<ModContext>();

    /// <summary>
    /// Get the full mod by key from the universe.
    /// </summary>
    public static ModPackage GetMod(this Universe universe, string modOrResourceKey)
      => universe.GetMods()
        .TryToGetModPackage(modOrResourceKey, out var found) 
          ? found 
          : throw new KeyNotFoundException($"Could not find mod package from key: {modOrResourceKey}");
  }
}
