using HarmonyLib;

namespace ExpandWorld;

[HarmonyPatch(typeof(Terminal), nameof(Terminal.InitTerminal))]
public class SetCommands
{
  static void Postfix()
  {
    new Terminal.ConsoleCommand("ew_spawns", "Forces spawn file creation.", (args) =>
{
  Spawn.Manager.Save();
}, true);
  }
}