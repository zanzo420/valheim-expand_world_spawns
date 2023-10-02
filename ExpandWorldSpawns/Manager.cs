using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ExpandWorldData;
using HarmonyLib;

namespace ExpandWorld.Spawn;

public class Manager
{
  public static string FileName = "expand_spawns.yaml";
  public static string FilePath = Path.Combine(EWD.YamlDirectory, FileName);
  public static string Pattern = "expand_spawns*.yaml";
  public static List<SpawnSystem.SpawnData> Originals = [];

  public static bool IsValid(SpawnSystem.SpawnData spawn) => spawn.m_prefab;
  public static string Save()
  {
    var spawnSystem = SpawnSystem.m_instances.FirstOrDefault();
    if (spawnSystem == null) return "";
    var spawns = spawnSystem.m_spawnLists.SelectMany(s => s.m_spawners);
    var yaml = DataManager.Serializer().Serialize(spawns.Select(Loader.ToData).ToList());
    File.WriteAllText(FilePath, yaml);
    return yaml;
  }
  public static void ToFile()
  {
    if (Helper.IsClient()) return;
    if (Originals.Count == 0)
    {
      var spawnSystem = SpawnSystem.m_instances.FirstOrDefault();
      Originals = spawnSystem.m_spawnLists.SelectMany(s => s.m_spawners).ToList();
    }
    if (File.Exists(FilePath)) return;
    var yaml = Save();
    Configuration.valueSpawnData.Value = yaml;
  }
  public static void FromFile()
  {
    if (Helper.IsClient()) return;
    var yaml = DataManager.Read(Pattern);
    Configuration.valueSpawnData.Value = yaml;
    Set(yaml);
  }
  public static void FromSetting(string yaml)
  {
    if (Helper.IsClient()) Set(yaml);
  }
  private static void Set(string yaml)
  {
    HandleSpawnData.Override = null;

    Loader.Data.Clear();
    Loader.Objects.Clear();
    if (yaml == "") return;
    try
    {
      var data = DataManager.Deserialize<Data>(yaml, FileName)
        .Select(Loader.FromData).Where(IsValid).ToList();
      if (data.Count == 0)
      {
        EWS.LogWarning($"Failed to load any spawn data.");
        return;
      }
      if (ExpandWorldData.Configuration.DataMigration && Helper.IsServer() && AddMissingEntries(data))
      {
        // Watcher triggers reload.
        return;
      }
      EWS.LogInfo($"Reloading spawn data ({data.Count} entries).");
      HandleSpawnData.Override = data;
      SpawnSystem.m_instances.ForEach(HandleSpawnData.Set);
    }
    catch (Exception e)
    {
      EWS.LogError(e.Message);
      EWS.LogError(e.StackTrace);
    }
  }

  private static bool AddMissingEntries(List<SpawnSystem.SpawnData> entries)
  {
    var missingKeys = Originals.Select(s => s.m_prefab.name).Distinct().ToHashSet();
    foreach (var item in entries)
      missingKeys.Remove(item.m_prefab.name);
    if (missingKeys.Count == 0) return false;
    var missing = Originals.Where(item => missingKeys.Contains(item.m_prefab.name)).ToList();
    EWS.LogWarning($"Adding {missing.Count} missing spawns to the expand_spawns.yaml file.");
    foreach (var item in missing)
      EWS.LogWarning(item.m_prefab.name);
    var yaml = File.ReadAllText(FilePath);
    var data = DataManager.Serializer().Serialize(missing.Select(Loader.ToData));
    // Directly appending is risky but necessary to keep comments, etc.
    yaml += "\n" + data;
    File.WriteAllText(FilePath, yaml);
    return true;
  }
  public static void SetupWatcher()
  {
    DataManager.SetupWatcher(Pattern, FromFile);
  }


}

[HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.Start)), HarmonyPriority(Priority.VeryLow)]
public class InitializeContent
{
  static void Postfix()
  {
    if (Helper.IsServer())
    {
      Manager.FromFile();
      // Spawn data is handled elsewhere.
    }
  }
}

[HarmonyPatch(typeof(SpawnSystem), nameof(SpawnSystem.Awake))]
public class HandleSpawnData
{
  // File exist check might be bit too slow for constant checking.
  static bool Done = false;
  public static List<SpawnSystem.SpawnData>? Override = null;
  static void Postfix(SpawnSystem __instance)
  {
    if (ZNet.instance.IsServer() && !Done)
    {
      Manager.ToFile();
      Done = true;
    }
    Set(__instance);
  }

  public static void Set(SpawnSystem system)
  {
    if (Override != null)
    {
      while (system.m_spawnLists.Count > 1)
        system.m_spawnLists.RemoveAt(system.m_spawnLists.Count - 1);
      system.m_spawnLists[0].m_spawners = Override;
    }
  }
}
