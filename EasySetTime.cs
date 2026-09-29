using HarmonyLib;
using Platform;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;

namespace EasySetTime
{
    public class ModApi : IModApi
    {
        public void InitMod(Mod _modInstance)
        {
            var harmony = new Harmony("scan_easysettime");
            harmony.PatchAll();

            Debug.Log("[EasySetTime] Loaded.");
        }
    }

    [Preserve]
    public class ConsoleCmdTime : ConsoleCmdAbstract
    {
        public override int DefaultPermissionLevel => 1000;

        public override DeviceFlag AllowedDeviceTypes => DeviceFlag.StandaloneWindows;

        public override string[] getCommands()
        {
            return new string[] { "time" };
        }

        public override string getDescription()
        {
            return "Adjusts the game time by N hours and M minutes. Usage: time <hours> [minutes]";
        }

        public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
        {
            var console = SingletonMonoBehaviour<SdtdConsole>.Instance;

            if (_params.Count < 1 || _params.Count > 2)
            {
                console.Output("Usage: time <hours> [minutes]   (negative values move time backwards)");
                return;
            }

            if (!int.TryParse(_params[0], out int hours))
            {
                console.Output($"'{_params[0]}' is not a valid integer.");
                return;
            }

            int minutes = 0;
            if (_params.Count == 2 && !int.TryParse(_params[1], out minutes))
            {
                console.Output($"'{_params[1]}' is not a valid integer.");
                return;
            }

            World world = GameManager.Instance.World;
            if (world == null)
            {
                console.Output("No world loaded.");
                return;
            }

            ulong worldTimeCurrent = world.worldTime;
            var (days, hoursNow, minutesNow) = GameUtils.WorldTimeToElements(worldTimeCurrent);

            int totalMinutes = hoursNow * 60 + minutesNow + hours * 60 + minutes;

            int newDay = days;
            while (totalMinutes < 0)
            {
                totalMinutes += 24 * 60;
                newDay--;
            }
            while (totalMinutes >= 24 * 60)
            {
                totalMinutes -= 24 * 60;
                newDay++;
            }

            if (newDay < 1)
            {
                console.Output("Cannot move time back before Day 1.");
                return;
            }

            int newHour = totalMinutes / 60;
            int newMinute = totalMinutes % 60;

            ulong newWorldTime = GameUtils.DayTimeToWorldTime(newDay, newHour, newMinute);
            world.SetTimeJump(newWorldTime);

            string delta = _params.Count == 2
                ? $"{hours}h {minutes}m"
                : $"{hours}h";

            console.Output(
                $"Time adjusted ({delta}). Current game time: " +
                ValueDisplayFormatters.WorldTime(newWorldTime, "Day {0}, {1:00}:{2:00}"));
        }
    }
}