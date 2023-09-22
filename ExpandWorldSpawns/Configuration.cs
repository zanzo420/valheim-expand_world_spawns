using ServerSync;
using Service;

namespace ExpandWorld;
public partial class Configuration
{
#nullable disable

  public static CustomSyncedValue<string> valueSpawnData;
#nullable enable
  public static void Init(ConfigWrapper wrapper)
  {
    valueSpawnData = wrapper.AddValue("spawn_data");
    valueSpawnData.ValueChanged += () => Spawn.Manager.FromSetting(valueSpawnData.Value);
  }
}
