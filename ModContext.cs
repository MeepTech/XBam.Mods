using Meep.Tech.Collections.Generic;
using Meep.Tech.XBam.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Meep.Tech.XBam.Mods.Configuration {
  public class ModContext : Universe.ExtraContext {
    readonly string _rootApplicationPersistentDataFolder;
    private readonly IEnumerable<string> _limitToModPackageKeys;
    Dictionary<Assembly, string> _modPackagesByAssemblyPlugins
      = new();

    /// <summary>
    /// File seperation characters
    /// </summary>
    public static readonly IEnumerable<char> FileSeperators 
      = new HashSet<char>() {'/', '\\'};

    /// <summary>
    /// The base mod folder name
    /// </summary>
    public const string ModFolderName
      = "mods";

    /// <summary>
    /// The root mod folder, where mods are loaded from.
    /// </summary>
    public string RootModsFolder {
      get;
    }

    /// <summary>
    /// All the mods that were imported.
    /// </summary>
    public IReadOnlyDictionary<string, ModPackage> ImportedMods
      => _importedMods; readonly Dictionary<string, ModPackage> _importedMods
        = new();

    /// <summary>
    /// All the assemblies that were imported by mods.
    /// </summary>
    public IEnumerable<Assembly> ImportedPlugins
      => _importedPlugins; readonly HashSet<Assembly> _importedPlugins
        = new();

    /// <summary>
    /// Make new mod porter settings to add to a universe.
    /// </summary>
    /// <param name="rootApplicationPersistentDataFolder">The directory to put the mods and data folders inside of</param>
    public ModContext([NotNull] string rootApplicationPersistentDataFolder, IEnumerable<string> limitToModPackageKeys = null) {
      _rootApplicationPersistentDataFolder = rootApplicationPersistentDataFolder;
      _limitToModPackageKeys = limitToModPackageKeys;

      RootModsFolder = Path.Combine(rootApplicationPersistentDataFolder, ModFolderName);
    }

    /// <summary>
    /// Get the mod package based on a resource in the mod.
    /// </summary>
    public bool TryToGetModPackageForResource(string resourceKey, out ModPackage modPackage)
      => ImportedMods.TryGetValue(resourceKey.Split(ModPackage.KeySeperator).First(), out modPackage);

    /// <summary>
    /// Get the mod package based on a mod package or resource key
    /// </summary>
    public bool TryToGetModPackage(string modOrResourceKey, out ModPackage modPackage) 
      => ImportedMods.TryGetValue(modOrResourceKey, out modPackage)
        || TryToGetModPackageForResource(modOrResourceKey, out modPackage);

    /// <summary>
    /// Get the name of the mod package based on the folder of the item creating the mod.
    /// </summary>
    protected virtual string GetModPackageKey(string modResourceFileLocation) {
      var localModFile = modResourceFileLocation[RootModsFolder.Length..];
      if (FileSeperators.Contains(localModFile[0])) {
        localModFile = localModFile.Trim(FileSeperators.ToArray());
      }

      return localModFile.Split(FileSeperators.ToArray()).First();
    }

    /// <summary>
    /// Overrideable callback to do something when an archetype is registered from a plugin.
    /// </summary>
    protected virtual void OnArchetypeRegisteredFromPlugin(Archetype archetype, ModPackage modPackage) { }

    /// <summary>
    /// Overrideable callback to do something when a model type is registered from a plugin.
    /// </summary>
    protected virtual void OnModelTypeRegisteredFromPlugin(System.Type archetype, ModPackage modPackage) { }

    /// <summary>
    /// Overrideable callback to do something when an enumeration is registered from a plugin.
    /// </summary>
    protected virtual void OnEnumerationRegisteredFromPlugin(Enumeration enumeration, ModPackage modPackage) { }

    /// <summary>
    /// Overrideable callback to do something when a component type is registered from a plugin.
    /// </summary>
    protected virtual void OnComponentTypeRegisteredFromPlugin(System.Type componentType, ModPackage modPackage) { }

    /// <summary>
    /// set up the initial loader configuation settings
    /// </summary>
    protected override Action<Loader> OnLoaderInitializationStart => loader => {
      loader.Options.DataFolderParentFolderLocation = _rootApplicationPersistentDataFolder;
      if (_limitToModPackageKeys is not null) {
        _addModPluginAssemblies(_limitToModPackageKeys, indexOffset: Universe.Loader.Options.PreLoadAssemblies.Count);
      } else {
        _addAllModPluginAssemblies(indexOffset: Universe.Loader.Options.PreLoadAssemblies.Count);
      }
    };

    /// <summary>
    /// Set mod assemblies to load in order by a list of mods.
    /// </summary>
    void _addModPluginAssemblies(IEnumerable<string> modPackageKeys, int indexOffset = 1) {
      int modIndex = indexOffset;
      modPackageKeys.Select(k => Path.Combine(RootModsFolder, k, ModPackage.PluginsSubFolderName))
        .SelectMany(pluginsFolder => Directory.GetFiles(pluginsFolder, "*.dll")
        .Where(pluginFileName => {
          string trimmedFileName = Path.GetFileName(pluginFileName);
          return !trimmedFileName.StartsWith(".")
            && !trimmedFileName.StartsWith("_");
        })
      ).ForEach(modAssemblyFile => {
        string packageKey = GetModPackageKey(modAssemblyFile);
        if (!ImportedMods.ContainsKey(packageKey)) {
          _addNewModPackage(packageKey);
        }

        Universe.Loader.Options.PreOrderedAssemblyFiles.Add((ushort)(modIndex++), modAssemblyFile);
      });
    }

    /// <summary>
    /// Loads all pluigns for all mods in the default mods folder in order discovered.
    /// </summary>
    void _addAllModPluginAssemblies(int indexOffset = 1) {
      int modIndex = indexOffset;
      IEnumerable<string> modFolders = Directory.GetDirectories(RootModsFolder)
        .Where(
          d => {
            var p = new DirectoryInfo(d).Name;
            return !p.StartsWith(".")
              && !p.StartsWith("_");
          });

      foreach (string modFolderLocation in modFolders) {
        string pluginsPath = Path.Combine(modFolderLocation, ModPackage.PluginsSubFolderName);
        if (Directory.Exists(pluginsPath)) {
          List<string> assemblies = Directory.GetFiles(
            pluginsPath,
            "*.dll",
            SearchOption.AllDirectories
          ).OrderBy(f => f.Replace(".", "")).ToList();
          foreach (string pluginFileLocation in assemblies) {
            if (Path.GetFileName(pluginFileLocation) is string f && !f.StartsWith(".") && (!f.StartsWith("_"))) {
              string packageKey = GetModPackageKey(pluginFileLocation);
              if (!ImportedMods.ContainsKey(packageKey)) {
                _addNewModPackage(packageKey);
              }

              Universe.Loader.Options.PreOrderedAssemblyFiles.Add((ushort)(modIndex++), pluginFileLocation);
            }
          }
        }
      }
    }

    protected override Action<bool, Type, Archetype, Exception, bool> OnLoaderArchetypeInitializationComplete
      => (success, type, archetype, error, isSplayed) => {
        if (success) {
          if (!Universe.Loader.CoreAssemblies.Contains(type.Assembly)) {
            string packageKey = _modPackagesByAssemblyPlugins.TryToGet(type.Assembly) 
              ?? (_modPackagesByAssemblyPlugins[type.Assembly] = GetModPackageKey(type.Assembly.Location));

            if (!_importedMods.TryGetValue(packageKey, out var modPackage)) {
              modPackage = _addNewModPackage(packageKey);
            }

            modPackage._addArchetypeFromPlugin(archetype);
            OnArchetypeRegisteredFromPlugin(archetype, modPackage);
          }
        }
      };

    protected override Action<bool, Type, Exception> OnLoaderModelFullInitializationComplete
      => (success, type, error) => {
        if (success) {
          if (!Universe.Loader.CoreAssemblies.Contains(type.Assembly)) {
            string packageKey = _modPackagesByAssemblyPlugins.TryToGet(type.Assembly)
              ?? (_modPackagesByAssemblyPlugins[type.Assembly] = GetModPackageKey(type.Assembly.Location));

            if (!_importedMods.TryGetValue(packageKey, out var modPackage)) {
              modPackage = _addNewModPackage(packageKey);
            }

            modPackage._addModelTypeFromPlugin(type);
            OnModelTypeRegisteredFromPlugin(type, modPackage);
          }
        }
      };

    protected override Action<bool, Type, Exception> OnLoaderComponentInitializationComplete
      => (success, type, error) => {
        if (success) {
          if (!Universe.Loader.CoreAssemblies.Contains(type.Assembly)) {
            string packageKey = _modPackagesByAssemblyPlugins.TryToGet(type.Assembly)
              ?? (_modPackagesByAssemblyPlugins[type.Assembly] = GetModPackageKey(type.Assembly.Location));

            if (!_importedMods.TryGetValue(packageKey, out var modPackage)) {
              modPackage = _addNewModPackage(packageKey);
            }

            modPackage._addComponentTypeFromPlugin(type);
            OnComponentTypeRegisteredFromPlugin(type, modPackage);
          }
        }
      };

    protected override Action<bool, PropertyInfo, Enumeration, Exception> OnLoaderEnumInitializationComplete 
      => (success, property, enumeration, error) => {
        if (success) {
          if (!Universe.Loader.CoreAssemblies.Contains(property.DeclaringType.Assembly)) {
            string packageKey = _modPackagesByAssemblyPlugins.TryToGet(property.DeclaringType.Assembly)
              ?? (_modPackagesByAssemblyPlugins[property.DeclaringType.Assembly] = GetModPackageKey(property.DeclaringType.Assembly.Location));

            if (!_importedMods.TryGetValue(packageKey, out var modPackage)) {
              modPackage = _addNewModPackage(packageKey);
            }

            modPackage._addEnumerationFromPlugin(enumeration);
            OnEnumerationRegisteredFromPlugin(enumeration, modPackage);
          }
        }
      };

    ///<summary><inheritdoc/></summary>
    protected override Action<Archetype> OnUnloadArchetype => archetype => {
      // remove from mod assets.
      if (ImportedPlugins.Contains(archetype.Type.Assembly)) {
        ImportedMods[_modPackagesByAssemblyPlugins[archetype.Type.Assembly]]
          .RemoveResource(archetype);
      }
    };

    ModPackage _addNewModPackage(string packageKey) {
      ModPackage modPackage = new ModPackage(packageKey, Universe);
      _importedMods.Add(packageKey, modPackage);
      return modPackage;
    }

    internal void _addPlugin(Assembly plugin) {
      _importedPlugins.Add(plugin);
    }
  }
}
