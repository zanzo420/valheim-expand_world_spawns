using HarmonyLib;

namespace ExpandWorld;
public class DebugCommands
{

  public DebugCommands()
  {
    new Terminal.ConsoleCommand("ew_spawns", "Forces spawn file creation.", (args) =>
    {
      Spawn.Manager.Save();
    }, true);
  }
}

[HarmonyPatch(typeof(Terminal), nameof(Terminal.InitTerminal))]
public class SetCommands
{
  static void Postfix()
  {
    new DebugCommands();
  }
}