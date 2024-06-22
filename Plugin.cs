using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TShockAPI;
using Terraria;
using TerrariaApi.Server;
using Microsoft.Xna.Framework;
using System.Text.Json;

namespace BossHPFix
{
    [ApiVersion(2, 1)]
    public class BossHPFix : TerrariaPlugin
    {

        public override string Author => "Onusai";
        public override string Description => "Modifies boss HP scaling. Ignores dead players and (if enabled) players from other teams";
        public override string Name => "BossHPFix";
        public override Version Version => new Version(1, 0, 0, 0);


        public class ConfigData
        {
            public bool UseTeamPlayerCount { get; set; } = true;
            // if set to false, all alive players will be counted
        }

        ConfigData configData;

        public BossHPFix(Main game) : base(game) { }

        public override void Initialize()
        {
            configData = PluginConfig.Load("BossHPFix");
            ServerApi.Hooks.GameInitialize.Register(this, OnGameLoad);
        }

        void OnGameLoad(EventArgs e)
        {
            ServerApi.Hooks.NpcSpawn.Register(this, OnNpcSpawn);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.GameInitialize.Deregister(this, OnGameLoad);
            }
            base.Dispose(disposing);
        }

        void RegisterCommand(string name, string perm, CommandDelegate handler, string helptext)
        {
            TShockAPI.Commands.ChatCommands.Add(new Command(perm, handler, name)
            { HelpText = helptext });
        }


        void OnNpcSpawn(NpcSpawnEventArgs args)
        {
            
            NPC npc = Main.npc[args.NpcId];

            List<TSPlayer> players = GetAlivePlayers();
            if (players.Count < 1) return;

            if (configData.UseTeamPlayerCount)
            {
                float nearestDistance = -1;
                TSPlayer nearestPlayer = null;

                foreach (TSPlayer p in players)
                {
                    float dist = p.LastNetPosition.Distance(npc.position);
                    if (nearestDistance == -1 || dist < nearestDistance)
                    {
                        nearestPlayer = p;
                        nearestDistance = dist;
                    }
                }

                int playersOnTeam = 0;

                foreach (TSPlayer p in players)
                {
                    if (p.Team == nearestPlayer.Team)
                    {
                        playersOnTeam++;
                    }
                }

                npc.SetDefaults(npc.type, new NPCSpawnParams { playerCountForMultiplayerDifficultyOverride = playersOnTeam });
            }
            else
            {
                npc.SetDefaults(npc.type, new NPCSpawnParams { playerCountForMultiplayerDifficultyOverride = players.Count });
            }
        }

        List<TSPlayer> GetAlivePlayers()
        {
            var players = new List<TSPlayer>();
            foreach (TSPlayer player in TShock.Players)
            {
                if (player != null && !player.Dead) players.Add(player);
            }
            return players;
        }

        public static class PluginConfig
        {
            public static string filePath;
            public static ConfigData Load(string Name)
            {
                filePath = String.Format("{0}/{1}.json", TShock.SavePath, Name);

                if (!File.Exists(filePath))
                {
                    var data = new ConfigData();
                    Save(data);
                    return data;
                }

                var jsonString = File.ReadAllText(filePath);
                var myObject = JsonSerializer.Deserialize<ConfigData>(jsonString);

                return myObject;
            }

            public static void Save(ConfigData myObject)
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var jsonString = JsonSerializer.Serialize(myObject, options);

                File.WriteAllText(filePath, jsonString);
            }
        }
    }
}