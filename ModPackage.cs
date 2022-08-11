using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Meep.Tech.XBam.Mods {

  /// <summary>
  /// Contains metadata about a mod-package.
  /// Mod packages are anything not included in the loader provided assemblies, the current assembly, or assemblies referenced by those
  /// </summary>
  public class ModPackage {
    Dictionary<string, object> _resources
      = new();

    /// <summary>
    /// Used to seperate the mod package key from the individual items resource index
    /// </summary>
    public const string KeySeperator = "::";

    /// <summary>
    /// Used to denote the folder for plugins
    /// </summary>
    public const string PluginsSubFolderName = "_plugins";

    /// <summary>
    /// The unique name of this mod package
    /// </summary>
    public string Key {
      get;
    }

    /// <summary>
    /// The universe this is for
    /// </summary>
    public Universe Universe {
      get;
    }

    /// <summary>
    /// The full names of the assembly plugins imported via this mod.
    /// </summary>
    public IEnumerable<Assembly> PluginAssembles
      => PluginAssembles; HashSet<Assembly> _pluginAssemblies
        = new();

    /// <summary>
    /// The full list of resource keys for resources imported by this mod.
    /// </summary>
    public IEnumerable<string> ResourceKeys
      => _resources.Keys;

    /// <summary>
    /// The archetypes imported by this mod.
    /// </summary>
    public virtual IEnumerable<Archetype> ImportedArchetypes
      => ImportedPluginBasedArchetypes;

    /// <summary>
    /// The archetypes imported by this mod from any pluign assemblies.
    /// </summary>
    public IEnumerable<Archetype> ImportedPluginBasedArchetypes
      => _importedPluginArchetypes; internal HashSet<Archetype> _importedPluginArchetypes
        = new();

    /// <summary>
    /// The archetypes imported by this mod from any pluign assemblies.
    /// </summary>
    public IEnumerable<Archetype> ImportedPluginBasedArchetypeBaseTypes
      => _importedPluginBaseArchetypes; internal HashSet<Archetype> _importedPluginBaseArchetypes
        = new();

    /// <summary>
    /// The models imported by this mod from any pluign assemblies.
    /// </summary>
    public virtual IEnumerable<System.Type> ImportedModelTypes
      => ImportedPluginBasedModelTypes;

    /// <summary>
    /// The models imported by this mod from any pluign assemblies.
    /// </summary>
    public IEnumerable<System.Type> ImportedPluginBasedModelTypes
      => _importedPluginModels; internal HashSet<System.Type> _importedPluginModels
        = new();

    /// <summary>
    /// The models imported by this mod from any pluign assemblies.
    /// </summary>
    public IEnumerable<System.Type> ImportedPluginBasedModelBaseTypes
      => _importedPluginBaseModels; internal HashSet<System.Type> _importedPluginBaseModels
        = new();

    /// <summary>
    /// The components imported by this mod from any pluign assemblies.
    /// </summary>
    public virtual IEnumerable<System.Type> ImportedComponentTypes
      => ImportedPluginBasedComponentTypes;

    /// <summary>
    /// The components imported by this mod from any pluign assemblies.
    /// </summary>
    public IEnumerable<System.Type> ImportedPluginBasedComponentTypes
      => _importedPluginComponents; internal HashSet<System.Type> _importedPluginComponents
        = new();

    /// <summary>
    /// The components imported by this mod from any pluign assemblies.
    /// </summary>
    public IEnumerable<System.Type> ImportedPluginBasedComponentBaseTypes
      => _importedPluginBaseComponents; internal HashSet<System.Type> _importedPluginBaseComponents
        = new();

    /// <summary>
    /// The enumerations imported by this mod from any pluign assemblies.
    /// </summary>
    public virtual IEnumerable<Enumeration> ImportedEnumerations
      => ImportedPluginBasedEnumerations;

    /// <summary>
    /// The enumerations imported by this mod from any pluign assemblies.
    /// </summary>
    public IEnumerable<Enumeration> ImportedPluginBasedEnumerations
      => _importedPluginEnumerations; internal HashSet<Enumeration> _importedPluginEnumerations
        = new();

    /// <summary>
    /// The enumerations imported by this mod from any pluign assemblies.
    /// </summary>
    public IEnumerable<Enumeration> ImportedPluginBasedEnumerationBaseTypes
      => _importedPluginBaseEnumerations;

    internal HashSet<Enumeration> _importedPluginBaseEnumerations
        = new();

    /// <summary>
    /// Used to make new modpackages.
    /// If something extends this it must have the same parameters!
    /// </summary>
    protected internal ModPackage(string key, Universe universe) {
      Key = key;
      Universe = universe;  
    }

    /// <summary>
    /// Used to make a key to find this archetype via this mod package.
    /// </summary>
    public string MakeResourceKey(Enumeration enumeration)
      => Key + KeySeperator + enumeration.ExternalId.ToString();

    /// <summary>
    /// Used to make a key to find this archetype via this mod package.
    /// </summary>
    public string MakeResourceKey(Archetype archetype)
      => Key + KeySeperator + archetype.Id;

    /// <summary>
    /// Used to make a key to find this archetype via this mod package.
    /// </summary>
    public string MakeResourceKey(System.Type modelOrComponentType)
      => Key + KeySeperator + modelOrComponentType.FullName;

    #region Get

    /// <summary>
    /// Get an archetype by it's resource key.
    /// </summary>
    public Archetype GetArchetype(string resourceKey)
      => (Archetype)_resources[resourceKey];

    /// <summary>
    /// Try to get an archetype from a resource key.
    /// </summary>
    public bool TryToGetArchetype(string resourceKey, out Archetype archetype)
      => _resources.TryGetValue(resourceKey, out var found)
      ? ((archetype = (found as Archetype)) != null)
      : (archetype = null) is not null;

    /// <summary>
    /// Get an enumeration by it's resource key.
    /// </summary>
    public Enumeration GetEnumeration(string resourceKey)
      => (Enumeration)_resources[resourceKey];

    /// <summary>
    /// Try to get an enumeration from a resource key.
    /// </summary>
    public bool TryToGetEnumeration(string resourceKey, out Enumeration archetype)
      => _resources.TryGetValue(resourceKey, out var found)
      ? ((archetype = (found as Enumeration)) != null)
      : (archetype = null) is not null;

    /// <summary>
    /// Get an model type by it's resource key.
    /// </summary>
    public System.Type GetModelType(string resourceKey) {
      System.Type type = (System.Type)_resources[resourceKey];
      return typeof(IModel).IsAssignableFrom(type) 
        && !typeof(IComponent).IsAssignableFrom(type)
          ? (System.Type)_resources[resourceKey]
          : throw new InvalidCastException($"Resource with key: {resourceKey} is not a Model Type class, or it is a Component Type class");
    }

    /// <summary>
    /// Try to get a model type from a resource key.
    /// </summary>
    public bool TryToGetModelType(string resourceKey, out System.Type modelType) {
      modelType = _resources.TryGetValue(resourceKey, out var foundType)
        ? foundType as System.Type
        : null;
      if (modelType is null) {
        return false;
      }

      if (typeof(IModel).IsAssignableFrom(modelType)
        && !typeof(IComponent).IsAssignableFrom(modelType)
      ) {
        return true;
      }

      modelType = null;
      return false;
    }

    /// <summary>
    /// Get an component type by it's resource key.
    /// </summary>
    public System.Type GetComponentType(string resourceKey) {
      System.Type type = (System.Type)_resources[resourceKey];
      return typeof(IComponent).IsAssignableFrom(type)
          ? (System.Type)_resources[resourceKey]
          : throw new InvalidCastException($"Resource with key: {resourceKey} is not a Component Type class");
    }

    /// <summary>
    /// Try to get a component type from a resource key.
    /// </summary>
    public bool TryToGetComponentType(string resourceKey, out System.Type modelType) {
      modelType = _resources.TryGetValue(resourceKey, out var foundType)
        ? foundType as System.Type
        : null;
      if (modelType is null) {
        return false;
      }

      if (typeof(IComponent).IsAssignableFrom(modelType)) {
        return true;
      }

      modelType = null;
      return false;
    }

    /// <summary>
    /// Used to get a resource by it's resource key.
    /// </summary>
    public object GetResourceByKey(string Key)
      => _resources[Key];

    /// <summary>
    /// Used to get a resource by it's resource key.
    /// </summary>
    public bool TryToGetResourceByKey(string Key, out object resource)
      => _resources.TryGetValue(Key, out resource);


    #endregion

    #region Set

    /// <summary>
    /// Overrideable callback after a resource is added.
    /// </summary>
    protected virtual void OnResourceAdded(string key, object resource) { }

    /// <summary>
    /// Overrideable callback after a resource is removed.
    /// </summary>
    protected virtual void OnResourceRemoved(string key, object resource) { }

    /// <summary>
    /// Used to internally add resources to mod packages.
    /// </summary>
    protected void AddResource(string key, object resource) {
      _resources.Add(key, resource);
      OnResourceAdded(key, resource);
    }

    /// <summary>
    /// Used to remove a resource from a mod package.
    /// </summary>
    internal protected void RemoveResource(object resource) {
      var found = _resources.FirstOrDefault(r => r.Equals(resource));
      _resources.Remove(found.Key);

      Assembly? pluginAssembly = null;
      if (found.Value is Archetype archetype) {
        if (PluginAssembles.Contains(pluginAssembly = archetype.Type.Assembly)) {
          _importedPluginArchetypes.Remove(archetype);
          _importedPluginBaseArchetypes.Remove(archetype);
        }
      } else if (found.Value is System.Type modelOrComponentType) {
        if(PluginAssembles.Contains(pluginAssembly = modelOrComponentType.Assembly)) {
          if (typeof(IModel).IsAssignableFrom(modelOrComponentType)) {
            if (typeof(IComponent).IsAssignableFrom(modelOrComponentType)) {
              _importedPluginComponents.Remove(modelOrComponentType);
              _importedPluginBaseComponents.Remove(modelOrComponentType); 
            } else {
              _importedPluginModels.Remove(modelOrComponentType);
              _importedPluginBaseModels.Remove(modelOrComponentType);
            }
          }
        }
      } else if (found.Value is Enumeration enumeration) {
        if(PluginAssembles.Contains(pluginAssembly = enumeration.GetType().Assembly)) {
          _importedPluginEnumerations.Remove(enumeration);
          _importedPluginBaseEnumerations.Remove(enumeration);
        }
      }

      if (pluginAssembly is not null && PluginAssembles.Contains(pluginAssembly)) {
        if (!ImportedPluginBasedArchetypeBaseTypes.Any(a => a.Type.Assembly == pluginAssembly)
          && !ImportedPluginBasedModelBaseTypes.Any(t => t.Assembly == pluginAssembly)
          && !ImportedPluginBasedEnumerationBaseTypes.Any(e => e.GetType().Assembly == pluginAssembly)
          && !ImportedPluginBasedComponentBaseTypes.Any(t => t.Assembly == pluginAssembly)
        ) {
          _pluginAssemblies.Remove(pluginAssembly);
        }
      }

      OnResourceRemoved(found.Key, found.Value);
    }

    internal void _addArchetypeFromPlugin(Archetype archetype) {
      _addPlugin(archetype.Type.Assembly);
      AddResource(MakeResourceKey(archetype), archetype);
      _importedPluginArchetypes.Add(archetype);
      if (archetype.IsBaseArchetype) {
        _importedPluginBaseArchetypes.Add(archetype);
      }
    }

    internal void _addEnumerationFromPlugin(Enumeration enumeration) {
      _addPlugin(enumeration.GetType().Assembly);
      AddResource(MakeResourceKey(enumeration), enumeration);
      _importedPluginEnumerations.Add(enumeration);
      if (enumeration.EnumBaseType == enumeration.GetType()) {
        _importedPluginBaseEnumerations.Add(enumeration);
      }
    }

    internal void _addModelTypeFromPlugin(System.Type modelType) {
      _addPlugin(modelType.Assembly);
      AddResource(MakeResourceKey(modelType), modelType);
      _importedPluginModels.Add(modelType);
      if (modelType == Models.GetModelBaseType(modelType)) {
        _importedPluginBaseModels.Add(modelType);
      }
    }

    internal void _addComponentTypeFromPlugin(System.Type componentType) {
      _addPlugin(componentType.Assembly);
      AddResource(MakeResourceKey(componentType), componentType);
      _importedPluginComponents.Add(componentType);
      if (componentType == Components.GetComponentBaseType(componentType)) {
        _importedPluginBaseComponents.Add(componentType);
      }
    }

    void _addPlugin(Assembly plugin) {
      _pluginAssemblies.Add(plugin);
      Universe.GetMods()._addPlugin(plugin);
    }

    #endregion
  }
}
