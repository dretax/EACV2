using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Timers;
using Fougerite;
using Fougerite.Events;
using UnityEngine;

namespace EACV2
{
    public class EAC : Fougerite.Module
    {
        /// <summary>
        /// Global Variables
        /// </summary>

        public static IniParser cfg;
        public static IniParser Settings;
        public static IniParser DizzyWarns;
        public static IniParser FlyJumpWarns;
        public static IniParser SilentAimWarns;
        public static IniParser SpeedHackWarns;
        public static IniParser WallPlaceWarns;
        public static System.Random rnd;
        public static string ppath;
        public static List<ulong> Debug; 
        public static List<ulong> FConnected;
        public static List<ulong> NextWarned;
        public static List<ulong> Notified;
        public static List<ulong> SafePlayers;
        public static List<ulong> HighPings;
        public static System.IO.StreamWriter file;
        public static System.IO.StreamWriter file2;
        public static Dictionary<ulong, int> wallhack;
        public static Dictionary<ulong, int> shotgwallhack;
        public static Dictionary<ulong, Vector3> FlySuspect;
        public static Dictionary<ulong, int> FlySuspectC;
        public static Dictionary<ulong, int> SustainedDetection;

        public static Dictionary<Penalities, IniParser> PenalityInis;
        public static Dictionary<Penalities, int> PenalityWarns;
        public static Vector3 UnderPlayerAdjustement = new Vector3(0f, -1.15f, 0f);
        public static Vector3 Vector3Down = new Vector3(0f, -1f, 0f);
        public static Vector3 Vector3Up = new Vector3(0f, 1f, 0f);
        public const float distanceDown = 10f;
        public const string red = "[color #FF0000]";
        public const string yellow = "[color yellow]";
        public const string green = "[color green]";
        public const string orange = "[color #ffa500]";

        private static Timer _timer;

        public readonly IEnumerable<string> Guns = new string[]
        {
            "M4", "MP5A4", "9mm Pistol", "Hunting Bow", "Bolt Action Rifle", "Shotgun", "Pipe Shotgun", "HandCannon",
            "P250", "Revolver"
        };

        public enum Penalities
        {
            Dizzy,
            FlyJump,
            SilentAim,
            Speed,
            WallPlace
        }


        /// <summary>
        /// Global Config Options
        /// </summary>

        public static bool Speed = true;
        public static bool Walk = false;
        public bool FlyandJump = true;
        public bool Place = true;
        public bool Dizzy = true;
        public bool Teleport = true;
        public bool PlayerWall = true;
        public bool EntityWall = true;
        public bool DisableTeleportCheckonAdmins = true;
        public bool DisableTeleportCheckonMods = true;
        public bool EnableFlyCheckonMods = false;
        public bool EnableFlyCheckonAdmins = false;

        public int DizzyWarnings = 1;
        public int FlyJumpWarnings = 5;
        public int SilentAimWarnings = 2;
        public int SpeedWarnings = 5;
        public int EntityPlaceWarnings = 1;
        public int TimerC = 120000;
        public double DizzyDistance = 2.50;

        public static int PingToIgnore = 250;

        public static float walkspeedMinDistance = 6f;
        public static float walkspeedMaxDistance = 15f;
        public static float walkspeedDropIgnore = 8f;
        public static float speedMinDistance = 14f;
        public static float speedMaxDistance = 25f;
        public static float speedDropIgnore = 8f;

        public override string Name
        {
            get { return "EAC"; }
        }

        public override string Author
        {
            get { return "DreTaX"; }
        }

        public override string Description
        {
            get { return "EAC"; }
        }

        public override Version Version
        {
            get { return new Version("2.0"); }
        }

        public override void Initialize()
        {
            if (!File.Exists(Path.Combine(ModuleFolder, "CheatDetection.log"))) { File.Create(Path.Combine(ModuleFolder, "CheatDetection.log")).Dispose(); }
            if (!File.Exists(Path.Combine(ModuleFolder, "FlyJumpWarns.ini"))) { File.Create(Path.Combine(ModuleFolder, "FlyJumpWarns.ini")).Dispose(); }
            if (!File.Exists(Path.Combine(ModuleFolder, "SilentAimWarns.ini"))) { File.Create(Path.Combine(ModuleFolder, "SilentAimWarns.ini")).Dispose(); }
            if (!File.Exists(Path.Combine(ModuleFolder, "SpeedHackWarns.ini"))) { File.Create(Path.Combine(ModuleFolder, "SpeedHackWarns.ini")).Dispose(); }
            if (!File.Exists(Path.Combine(ModuleFolder, "WallPlaceWarns.ini"))) { File.Create(Path.Combine(ModuleFolder, "WallPlaceWarns.ini")).Dispose(); }
            if (!File.Exists(Path.Combine(ModuleFolder, "DizzyWarns.ini"))) { File.Create(Path.Combine(ModuleFolder, "DizzyWarns.ini")).Dispose(); }
            ppath = Path.Combine(ModuleFolder, "CheatDetection.log");
            FConnected = new List<ulong>();
            NextWarned = new List<ulong>();
            Notified = new List<ulong>();
            Debug = new List<ulong>();
            SafePlayers = new List<ulong>();
            HighPings = new List<ulong>();
            FlyJumpWarns = new IniParser(Path.Combine(ModuleFolder, "FlyJumpWarns.ini"));
            SilentAimWarns = new IniParser(Path.Combine(ModuleFolder, "SilentAimWarns.ini"));
            SpeedHackWarns = new IniParser(Path.Combine(ModuleFolder, "SpeedHackWarns.ini"));
            WallPlaceWarns = new IniParser(Path.Combine(ModuleFolder, "WallPlaceWarns.ini"));
            DizzyWarns = new IniParser(Path.Combine(ModuleFolder, "DizzyWarns.ini"));
            PenalityInis = new Dictionary<Penalities, IniParser>();
            PenalityInis[Penalities.FlyJump] = FlyJumpWarns;
            PenalityInis[Penalities.SilentAim] = SilentAimWarns;
            PenalityInis[Penalities.Speed] = SpeedHackWarns;
            PenalityInis[Penalities.WallPlace] = WallPlaceWarns;
            PenalityInis[Penalities.Dizzy] = DizzyWarns;

            PenalityWarns = new Dictionary<Penalities, int>();
            PenalityWarns[Penalities.FlyJump] = FlyJumpWarnings;
            PenalityWarns[Penalities.SilentAim] = SilentAimWarnings;
            PenalityWarns[Penalities.Speed] = SpeedWarnings;
            PenalityWarns[Penalities.WallPlace] = EntityPlaceWarnings;
            PenalityWarns[Penalities.Dizzy] = DizzyWarnings;
            rnd = new System.Random();
            cfg = new IniParser(Path.Combine(ModuleFolder, "DefaultLoc.ini"));
            wallhack = new Dictionary<ulong, int>();
            shotgwallhack = new Dictionary<ulong, int>();
            FlySuspect = new Dictionary<ulong, Vector3>();
            FlySuspectC = new Dictionary<ulong, int>();
            SustainedDetection = new Dictionary<ulong, int>();
            if (!File.Exists(Path.Combine(ModuleFolder, "Settings.ini")))
            {
                File.Create(Path.Combine(ModuleFolder, "Settings.ini")).Dispose();
                Settings = new IniParser(Path.Combine(ModuleFolder, "Settings.ini"));
                Settings.AddSetting("Settings", "TimerC", "120000");
                Settings.AddSetting("Settings", "FlyandJump", "true");
                Settings.AddSetting("Settings", "Speed", "true");
                Settings.AddSetting("Settings", "Place", "true");
                Settings.AddSetting("Settings", "Dizzy", "true");
                Settings.AddSetting("Settings", "Teleport", "true");
                Settings.AddSetting("Settings", "Walk", "false");
                Settings.AddSetting("Settings", "PlayerWall", "true");
                Settings.AddSetting("Settings", "EntityWall", "true");
                Settings.AddSetting("Settings", "DizzyDistance", DizzyDistance.ToString());
                Settings.AddSetting("Settings", "DizzyWarnings", DizzyWarnings.ToString());
                Settings.AddSetting("Settings", "FlyJumpWarnings", FlyJumpWarnings.ToString());
                Settings.AddSetting("Settings", "SilentAimWarnings", SilentAimWarnings.ToString());
                Settings.AddSetting("Settings", "SpeedWarnings", SpeedWarnings.ToString());
                Settings.AddSetting("Settings", "EntityPlaceWarnings", EntityPlaceWarnings.ToString());
                Settings.AddSetting("Settings", "PingToIgnore", PingToIgnore.ToString());
                Settings.AddSetting("Settings", "DisableTeleportCheckonAdmins", DisableTeleportCheckonAdmins.ToString());
                Settings.AddSetting("Settings", "DisableTeleportCheckonMods", DisableTeleportCheckonMods.ToString());
                Settings.AddSetting("Settings", "EnableFlyCheckonMods", EnableFlyCheckonMods.ToString());
                Settings.AddSetting("Settings", "EnableFlyCheckonAdmins", EnableFlyCheckonAdmins.ToString());
                Settings.AddSetting("Settings", "speedMinDistance", speedMinDistance.ToString());
                Settings.AddSetting("Settings", "speedMaxDistance", speedMaxDistance.ToString());
                Settings.AddSetting("Settings", "walkspeedMinDistance", walkspeedMinDistance.ToString());
                Settings.AddSetting("Settings", "walkspeedMaxDistance", walkspeedMaxDistance.ToString());
                Settings.Save();
            }
            else
            {
                Settings = new IniParser(Path.Combine(ModuleFolder, "Settings.ini"));
            }
            ReloadConfig();
            Fougerite.Hooks.OnPlayerSpawned += OnPlayerSpawned;
            Fougerite.Hooks.OnPlayerSpawning += OnPlayerSpawning;
            Fougerite.Hooks.OnPlayerConnected += OnPlayerConnected;
            Fougerite.Hooks.OnPlayerKilled += OnPlayerKilled;
            Fougerite.Hooks.OnPlayerDisconnected += OnPlayerDisconnected;
            Fougerite.Hooks.OnPlayerMove += OnPlayerMove;
            Fougerite.Hooks.OnCommand += OnCommand;
            Fougerite.Hooks.OnEntityHurt += OnEntityHurt;
            Fougerite.Hooks.OnPlayerHurt += OnPlayerHurt;
            Start();
        }

        public override void DeInitialize()
        {
            _timer.Dispose();
            Fougerite.Hooks.OnPlayerSpawned -= OnPlayerSpawned;
            Fougerite.Hooks.OnPlayerSpawning -= OnPlayerSpawning;
            Fougerite.Hooks.OnPlayerConnected -= OnPlayerConnected;
            Fougerite.Hooks.OnPlayerKilled -= OnPlayerKilled;
            Fougerite.Hooks.OnPlayerDisconnected -= OnPlayerDisconnected;
            Fougerite.Hooks.OnPlayerMove -= OnPlayerMove;
            Fougerite.Hooks.OnCommand -= OnCommand;
            Fougerite.Hooks.OnEntityHurt -= OnEntityHurt;
            Fougerite.Hooks.OnPlayerHurt -= OnPlayerHurt;
        }

        private void ReloadConfig()
        {
            Settings = new IniParser(Path.Combine(ModuleFolder, "Settings.ini"));
            try
            {
                PingToIgnore = int.Parse(Settings.GetSetting("Settings", "PingToIgnore"));
                DizzyWarnings = int.Parse(Settings.GetSetting("Settings", "DizzyWarnings"));
                FlyJumpWarnings = int.Parse(Settings.GetSetting("Settings", "FlyJumpWarnings"));
                SilentAimWarnings = int.Parse(Settings.GetSetting("Settings", "SilentAimWarnings"));
                SpeedWarnings = int.Parse(Settings.GetSetting("Settings", "SpeedWarnings"));
                EntityPlaceWarnings = int.Parse(Settings.GetSetting("Settings", "EntityPlaceWarnings"));
                DizzyDistance = double.Parse(Settings.GetSetting("Settings", "DizzyDistance"));
                FlyandJump = Settings.GetBoolSetting("Settings", "FlyandJump");
                Speed = Settings.GetBoolSetting("Settings", "Speed");
                Place = Settings.GetBoolSetting("Settings", "Place");
                Dizzy = Settings.GetBoolSetting("Settings", "Dizzy");
                Teleport = Settings.GetBoolSetting("Settings", "Teleport");
                Walk = Settings.GetBoolSetting("Settings", "Walk");
                PlayerWall = Settings.GetBoolSetting("Settings", "PlayerWall");
                EntityWall = Settings.GetBoolSetting("Settings", "EntityWall");
                DisableTeleportCheckonMods = Settings.GetBoolSetting("Settings", "DisableTeleportCheckonMods");
                DisableTeleportCheckonAdmins = Settings.GetBoolSetting("Settings", "DisableTeleportCheckonAdmins");
                EnableFlyCheckonMods = Settings.GetBoolSetting("Settings", "EnableFlyCheckonMods");
                EnableFlyCheckonAdmins = Settings.GetBoolSetting("Settings", "EnableFlyCheckonAdmins");
                TimerC = int.Parse(Settings.GetSetting("Settings", "TimerC"));

                FlyJumpWarns = new IniParser(Path.Combine(ModuleFolder, "FlyJumpWarns.ini"));
                SilentAimWarns = new IniParser(Path.Combine(ModuleFolder, "SilentAimWarns.ini"));
                SpeedHackWarns = new IniParser(Path.Combine(ModuleFolder, "SpeedHackWarns.ini"));
                WallPlaceWarns = new IniParser(Path.Combine(ModuleFolder, "WallPlaceWarns.ini"));
                DizzyWarns = new IniParser(Path.Combine(ModuleFolder, "DizzyWarns.ini"));
                PenalityInis = new Dictionary<Penalities, IniParser>();
                PenalityInis[Penalities.FlyJump] = FlyJumpWarns;
                PenalityInis[Penalities.SilentAim] = SilentAimWarns;
                PenalityInis[Penalities.Speed] = SpeedHackWarns;
                PenalityInis[Penalities.WallPlace] = WallPlaceWarns;
                PenalityInis[Penalities.Dizzy] = DizzyWarns;

                PenalityWarns = new Dictionary<Penalities, int>();
                PenalityWarns[Penalities.FlyJump] = FlyJumpWarnings;
                PenalityWarns[Penalities.SilentAim] = SilentAimWarnings;
                PenalityWarns[Penalities.Speed] = SpeedWarnings;
                PenalityWarns[Penalities.WallPlace] = EntityPlaceWarnings;
                PenalityWarns[Penalities.Dizzy] = DizzyWarnings;
            }
            catch (Exception ex)
            {
                Logger.LogError("EAC: Missing a config option. Delete your config & Restart. " + ex.Message);
            }
        }

        /*
         *
         * Timers
         *
         */

        private void Start()
        {
            _timer = new Timer(TimerC);
            _timer.Elapsed += new ElapsedEventHandler(RunOff);
            _timer.Enabled = true;
        }

        private void RunOff(object sender, ElapsedEventArgs e)
        {
            _timer.Dispose();
            CheckHacks();
            Start();
        }

        private void CheckHacks()
        {
            try
            {
                foreach (Fougerite.Player p in Server.GetServer().Players)
                {
                    if (p.Admin || p.Moderator)
                    {
                        continue;
                    }
                    EACChecker phandler = p.PlayerClient.netUser.playerClient.gameObject.GetComponent<EACChecker>();
                    if (phandler == null) { phandler = p.PlayerClient.netUser.playerClient.gameObject.AddComponent<EACChecker>(); }
                    phandler.timeleft = 3600f;
                    phandler.StartCheck();
                }
            }
            catch
            {
            }
        }

        /*
         *
         * Timers End
         *
         */


        /*
         *
         * Hooks
         *
         */

        public void OnPlayerDisconnected(Fougerite.Player player)
        {
            DataStore.GetInstance().Add("EACDizzy", player.UID, player.DisconnectLocation);
        }

        public void OnPlayerKilled(DeathEvent de)
        {
            if (de.VictimIsPlayer && de.Victim != null)
            {
                Fougerite.Player victim = (Fougerite.Player)de.Victim;
                if (FlySuspect.ContainsKey(victim.UID))
                {
                    FlySuspect.Remove(victim.UID);
                }
            }
        }

        public void OnPlayerMove(HumanController hc, Vector3 origin, int encoded, ushort stateflags, uLink.NetworkMessageInfo info)
        {
            if (!FlyandJump)
            {
                return;
            }
            Fougerite.Player player = Fougerite.Server.Cache.ContainsKey(hc.netUser.userID) ? Fougerite.Server.Cache[hc.netUser.userID]
                    : Fougerite.Server.GetServer().FindPlayer(hc.netUser.userID.ToString());
            if (player == null)
            {
                if (hc.netUser == null) return;
                if (hc.netUser.connected)
                {
                    hc.netUser.Kick(NetError.NoError, true);
                }
                return;
            }
            bool debugc = Debug.Contains(player.UID);
            if (!debugc)
            {
                if ((player.Admin && !EnableFlyCheckonAdmins) || (player.Moderator && !EnableFlyCheckonMods))
                {
                    return;
                }
            }
            if (!player.IsOnline)
            {
                return;
            }
            Vector3 newl = player.Location;
            float dist = Vector3.Distance(origin, newl) - 1.647583f;
            if (dist == 0)
            {
                return;
            }
            if (dist >= 26f)
            {
                return;
            }
            Vector3 pos = new Vector3(newl.x, World.GetWorld().GetGround(newl.x, newl.z), newl.z);
            float wdist = Vector3.Distance(pos, newl);
            if (wdist <= 4f)
            {
                return;
            }
            if (SafePlayers.Contains(player.UID))
            {
                SafePlayers.Remove(player.UID);
                if (SustainedDetection.ContainsKey(player.UID)) { SustainedDetection.Remove(player.UID); }
                if (FlySuspectC.ContainsKey(player.UID)) { FlySuspectC.Remove(player.UID); }
                if (FlySuspect.ContainsKey(player.UID)) { FlySuspect.Remove(player.UID); }
                return;
            }
            bool b = PlayerHandlerHasGround(newl);
            float newy = newl.y;
            string line;
            if (!b)
            {
                var x = Physics.OverlapSphere(newl + Vector3Up, 3f);
                if (debugc)
                {
                    foreach (var d in x)
                    {
                        player.MessageFrom("EAC DEBUG", "Found object near you: " + d.name);
                    }
                }
                if (x.Any(y => y.GetComponent<UnityEngine.MeshCollider>() != null))
                {
                    return;
                }
                if (IsOnSupportPos(newl + Vector3Up))
                {
                    if (debugc)
                    {
                        player.MessageFrom("EAC DEBUG", "Detected IsOnSupport");
                    }
                    return;
                }
                List<Vector3> vs = new List<Vector3>();
                vs.Add(new Vector3(newl.x - 1.0f, newl.y, newl.z));
                vs.Add(new Vector3(newl.x - 2.0f, newl.y, newl.z));
                vs.Add(new Vector3(newl.x - 3.0f, newl.y, newl.z));
                vs.Add(new Vector3(newl.x + 1.0f, newl.y, newl.z));
                vs.Add(new Vector3(newl.x + 2.0f, newl.y, newl.z));
                vs.Add(new Vector3(newl.x + 3.0f, newl.y, newl.z));
                vs.Add(new Vector3(newl.x, newl.y, newl.z - 1.0f));
                vs.Add(new Vector3(newl.x, newl.y, newl.z - 2.0f));
                vs.Add(new Vector3(newl.x, newl.y, newl.z - 3.0f));
                vs.Add(new Vector3(newl.x, newl.y, newl.z + 1.0f));
                vs.Add(new Vector3(newl.x, newl.y, newl.z + 2.0f));
                vs.Add(new Vector3(newl.x, newl.y, newl.z + 3.0f));
                if (vs.Any(PlayerHandlerHasGround))
                {
                    return;
                }
                if (!FlySuspect.ContainsKey(player.UID))
                {
                    FlySuspect.Add(player.UID, newl);
                }
                if (!FlySuspectC.ContainsKey(player.UID))
                {
                    FlySuspectC.Add(player.UID, 0);
                }
                else
                {
                    var olddist = FlySuspect[player.UID];
                    if (olddist.y <= newl.y || Vector3.Distance(olddist, newl) <= 2f)
                    {
                        FlySuspect[player.UID] = newl;
                        int c = FlySuspectC[player.UID] + 1;
                        FlySuspectC[player.UID] = c;
                        if (player.Ping >= PingToIgnore)
                        {
                            if (SafePlayers.Contains(player.UID))
                            {
                                SafePlayers.Remove(player.UID);
                                if (SustainedDetection.ContainsKey(player.UID)) { SustainedDetection.Remove(player.UID); }
                                if (FlySuspectC.ContainsKey(player.UID)) { FlySuspectC.Remove(player.UID); }
                                if (FlySuspect.ContainsKey(player.UID)) { FlySuspect.Remove(player.UID); }
                                return;
                            }
                            MessageAdmins(yellow + player.Name + " could be flying or jump hacking. High ping..");
                            line = DateTime.Now + " [Fly&Jump] POSSIBLE Fly/Jump hack usage at " + player.Name + " Ping: " +
                                   player.Ping + " | " + player.SteamID + " Loc: " + newl;
                            file = new System.IO.StreamWriter(ppath, true);
                            file.WriteLine(line);
                            file.Close();
                            if (!SustainedDetection.ContainsKey(player.UID))
                            {
                                SustainedDetection[player.UID] = 0;
                                return;
                            }
                            if (c >= 4)
                            {
                                SustainedDetection[player.UID] = SustainedDetection[player.UID] + 1;
                                return;
                            }
                            if (SustainedDetection[player.UID] >= 4)
                            {
                                if (SafePlayers.Contains(player.UID))
                                {
                                    SafePlayers.Remove(player.UID);
                                    if (SustainedDetection.ContainsKey(player.UID)) { SustainedDetection.Remove(player.UID); }
                                    if (FlySuspectC.ContainsKey(player.UID)) { FlySuspectC.Remove(player.UID); }
                                    if (FlySuspect.ContainsKey(player.UID)) { FlySuspect.Remove(player.UID); }
                                    return;
                                }
                                Warn(player, Penalities.FlyJump, true);
                            }
                            return;
                        }
                        if (olddist.y <= newy && c >= 4)
                        {
                            if (SafePlayers.Contains(player.UID))
                            {
                                SafePlayers.Remove(player.UID);
                                if (SustainedDetection.ContainsKey(player.UID)) { SustainedDetection.Remove(player.UID); }
                                if (FlySuspectC.ContainsKey(player.UID)) { FlySuspectC.Remove(player.UID); }
                                if (FlySuspect.ContainsKey(player.UID)) { FlySuspect.Remove(player.UID); }
                                return;
                            }
                            Server.GetServer()
                                .BroadcastFrom("EAC",
                                    red + "Detected Fly/Jump hack usage at " + player.Name + " Ping: " + player.Ping);
                            line = DateTime.Now + " [Fly&Jump] Detected Fly/Jump hack usage at " + player.Name + " Ping: " +
                                   player.Ping + " | " + player.SteamID + " Loc: " + newl;
                            file = new System.IO.StreamWriter(ppath, true);
                            file.WriteLine(line);
                            file.Close();
                            FlySuspectC.Remove(player.UID);
                            FlySuspect.Remove(player.UID);
                            Warn(player, Penalities.FlyJump, true);
                        }
                    }
                }
            }
            else
            {
                if (FlySuspectC.ContainsKey(player.UID))
                {
                    int c2 = FlySuspectC[player.UID] - 1;
                    if (c2 <= 0)
                    {
                        FlySuspectC.Remove(player.UID);
                        if (FlySuspect.ContainsKey(player.UID))
                        {
                            FlySuspect.Remove(player.UID);
                        }
                        return;
                    }
                    FlySuspectC[player.UID] = c2;
                }
            }
        }


        public void OnPlayerConnected(Fougerite.Player player)
        {
            if (!FConnected.Contains(player.UID))
            {
                FConnected.Add(player.UID);
            }
        }

        public void OnPlayerSpawning(Fougerite.Player player, SpawnEvent se)
        {
            if (DataStore.GetInstance().Get("DizzySpawn", player.UID) != null)
            {
                var loc = (Vector3) DataStore.GetInstance().Get("DizzySpawn", player.UID);
                se.Location = loc;
                DataStore.GetInstance().Remove("DizzySpawn", player.UID);
            }
        }

        public void OnPlayerSpawned(Fougerite.Player player, SpawnEvent se)
        {
            if (FConnected.Contains(player.UID))
            {
                if (DataStore.GetInstance().Get("EACDizzy", player.UID) != null)
                {
                    var dict = new Dictionary<string, object>();
                    dict["Player"] = player;
                    dict["dizzy"] = 1;
                    CreateParallelTimer(2000, dict).Start();
                }
                FConnected.Remove(player.UID);
            }
        }

        public void OnEntityHurt(HurtEvent he)
        {
            if (!EntityWall) { return; }
            if (he.AttackerIsPlayer)
            {
                if (he.DamageType != "Bullet" && he.DamageType != "Melee")
                {
                    return;
                }
                Entity entity = he.Entity;
                if (!entity.Name.Contains("Stash") && !entity.Name.Contains("Box"))
                {
                    return;
                }
                string ownerid = entity.OwnerID;
                Vector3 lloc = entity.Location;
                string ename = entity.Name;
                Fougerite.Player attacker = (Fougerite.Player)he.Attacker;
                if (attacker.IsDisconnecting)
                {
                    return;
                }
                Vector3 attackerloc = attacker.Location;
                Vector3 eyesOrigin = attacker.PlayerClient.controllable.character.eyesOrigin;
                Vector3 attackerdir = attacker.PlayerClient.controllable.character.eyesRay.direction;
                float num = Vector3.Distance(eyesOrigin, lloc);
                if ((int)Math.Round(num) == 0)
                {
                    return;
                }
                RaycastHit[] hitArray;
                bool flag = false;
                try
                {
                    hitArray = Physics.RaycastAll(eyesOrigin, attackerdir, num);
                }
                catch
                {
                    return;
                }
                foreach (RaycastHit hit in hitArray)
                {
                    string name = hit.collider.gameObject.name;
                    if ((name == "Terrain" || name == "__MESHBATCH_PHYSICAL_OUTPUT" || name.ToLower().Contains("barricade")) && (num - hit.distance > 0.25f))
                    {
                        flag = true;
                    }
                }
                int num2;
                bool s = false;
                if (he.WeaponName.Contains("Shotgun") || he.WeaponName.Contains("Cannon"))
                {
                    num2 = shotgwallhack.ContainsKey(attacker.UID) ? shotgwallhack[attacker.UID] : 0;
                    s = true;
                }
                else
                {
                    num2 = wallhack.ContainsKey(attacker.UID) ? wallhack[attacker.UID] : 0;
                }
                if (flag && !Notified.Contains(Convert.ToUInt64(entity.OwnerID)) && num2 > 1)
                {
                    var victim = Server.GetServer().FindPlayer(entity.OwnerID);
                    var nuid = Convert.ToUInt64(entity.OwnerID);
                    Notified.Add(nuid);
                    Dictionary<string, object> dd = new Dictionary<string, object>();
                    dd["notif"] = entity.OwnerID;
                    string line;
                    if (victim != null)
                    {
                        victim.MessageFrom("EAC",
                            orange + "You are possibly being targeted by " + yellow + attacker.Name);
                        victim.MessageFrom("EAC",
                            orange + "We detected Silent Aim Usage at him, stay behind walls so we can protect you.");
                        victim.MessageFrom("EAC", orange + "His actions were logged, report this to the admins.");
                        line = DateTime.Now + " [SilentAim] SUSPECTING ENTITY Silent Aim at " + attacker.Name + "(" + attacker.SteamID + ") , he tried to shoot " + victim.Name + " (" + victim.SteamID + ")" + "'s items through walls. From: " + attackerloc +
                               " to " + lloc + " Ping: " + attacker.Ping;
                    }
                    else
                    {
                        line = DateTime.Now + " [SilentAim] SUSPECTING ENTITY Silent Aim at " + attacker.Name + "(" + attacker.SteamID + ") , he tried to shoot " + entity.OwnerName + " (" + entity.OwnerID + ")" + "'s items through walls. From: " +
                               attackerloc + " to " + lloc + " Ping: " + attacker.Ping;
                    }
                    MessageAdmins(yellow + "Suspecting Silent Aim at " + attacker.Name + " Check the logs.");
                    file = new System.IO.StreamWriter(ppath, true);
                    file.WriteLine(line);
                    file.Close();
                    CreateParallelTimer(40000, dd).Start();
                }
                if (flag)
                {
                    he.DamageAmount = 0f;
                    if (s)
                    {
                        switch (num2)
                        {
                            case 0:
                            case 1:
                            case 2:
                            case 3:
                            case 4:
                            case 5:
                            case 6:
                            case 7:
                            case 8:
                            case 9:
                            case 10:
                            case 11:
                            case 12:
                                shotgwallhack[attacker.UID] = num2 + 1;
                                return;
                        }
                    }
                    else
                    {
                        switch (num2)
                        {
                            case 0:
                            case 1:
                            case 2:
                            case 3:
                            case 4:
                            case 5:
                                wallhack[attacker.UID] = num2 + 1;
                                return;
                        }
                    }
                    if (wallhack.ContainsKey(attacker.UID)) wallhack.Remove(attacker.UID);
                    if (shotgwallhack.ContainsKey(attacker.UID)) shotgwallhack.Remove(attacker.UID);
                    string line = DateTime.Now + " [SilentAim] Detected Entity Silent Aim at " + attacker.Name + "(" + attacker.SteamID + ") , he tried to shoot a " + ename + " through walls. OWNERID: "
                        + ownerid + " From: "
                        + attackerloc + " to " + lloc;
                    Server.GetServer().BroadcastFrom("EAC", red + "Detected Entity Silent Aim at " + attacker.Name);
                    file = new System.IO.StreamWriter(ppath, true);
                    file.WriteLine(line);
                    file.Close();
                    Warn(attacker, Penalities.SilentAim, true);
                }
                else if (num2 > 0)
                {
                    if (s)
                    {
                        shotgwallhack[attacker.UID] = num2 - 1;
                    }
                    else
                    {
                        wallhack[attacker.UID] = num2 - 1;
                    }
                }
            }
        }

        public void OnPlayerHurt(HurtEvent he)
        {
            if (!PlayerWall) { return; }
            try
            {
                if (he.AttackerIsPlayer)
                {
                    Fougerite.Player attacker = (Fougerite.Player)he.Attacker;
                    if (attacker.IsDisconnecting)
                    {
                        return;
                    }
                    string weapon = he.WeaponName;
                    if (!Guns.Contains(weapon))
                    {
                        return;
                    }
                    if (he.DamageType == "Bleeding")
                    {
                        return;
                    }
                    Vector3 attackerloc = attacker.Location;
                    Vector3 eyesOrigin = attacker.PlayerClient.controllable.character.eyesOrigin;
                    Vector3 attackerdir = attacker.PlayerClient.controllable.character.eyesRay.direction;
                    float num;
                    RaycastHit[] hitArray;
                    bool flag = false;
                    string vname;
                    Vector3 lloc;
                    int num2;
                    bool s = false;
                    if (he.WeaponName.Contains("Shotgun") || he.WeaponName.Contains("Cannon"))
                    {
                        num2 = shotgwallhack.ContainsKey(attacker.UID) ? shotgwallhack[attacker.UID] : 0;
                        s = true;
                    }
                    else
                    {
                        num2 = wallhack.ContainsKey(attacker.UID) ? wallhack[attacker.UID] : 0;
                    }
                    if (he.Sleeper)
                    {
                        Sleeper victim = (Sleeper)he.Victim;
                        vname = victim.Name;
                        lloc = victim.Location;
                        num = Vector3.Distance(eyesOrigin, lloc);
                        if ((int)Math.Round(num) == 0)
                        {
                            return;
                        }
                        try
                        {
                            hitArray = Physics.RaycastAll(eyesOrigin, attackerdir, num);
                        }
                        catch
                        {
                            return;
                        }
                        if ((from hit in hitArray let name = hit.collider.gameObject.name where (name == "Terrain" || name == "__MESHBATCH_PHYSICAL_OUTPUT") && (num - hit.distance > 0.25f) select hit).Any())
                        {
                            flag = true;
                        }
                    }
                    else
                    {
                        Fougerite.Player victim = (Fougerite.Player)he.Victim;
                        lloc = victim.Location;
                        Vector3 veyes = victim.PlayerClient.controllable.character.eyesOrigin;
                        vname = victim.Name;
                        num = Vector3.Distance(eyesOrigin, veyes);
                        if ((int)Math.Round(num) == 0)
                        {
                            return;
                        }
                        try
                        {
                            hitArray = Physics.RaycastAll(eyesOrigin, attackerdir, num);
                        }
                        catch
                        {
                            return;
                        }
                        if (hitArray.Select(hit => hit.collider.gameObject.name).Any(name => name == "Terrain" || name == "__MESHBATCH_PHYSICAL_OUTPUT" || name.ToLower().Contains("barricade")))
                        {
                            flag = true;
                        }
                        if (flag && !Notified.Contains(victim.UID) && num2 > 1)
                        {
                            Notified.Add(victim.UID);
                            Dictionary<string, object> dd = new Dictionary<string, object>();
                            dd["notif"] = victim.UID;
                            victim.MessageFrom("EAC", orange + "You are possibly being targeted by " + yellow + attacker.Name);
                            victim.MessageFrom("EAC", orange + "We detected Silent Aim Usage at him, stay behind walls so we can protect you.");
                            victim.MessageFrom("EAC", orange + "His actions were logged, report this to the admins.");
                            string line = DateTime.Now + " [SilentAim] SUSPECTING Silent Aim at " + attacker.Name + "(" + attacker.SteamID + ") , he tried to shoot " + vname + " through walls. From: " + attackerloc + " to " + lloc + " Ping: " + attacker.Ping;
                            MessageAdmins(yellow + "Suspecting Silent Aim at " + attacker.Name + " Check the logs.");
                            file = new System.IO.StreamWriter(ppath, true);
                            file.WriteLine(line);
                            file.Close();
                            CreateParallelTimer(40000, dd).Start();
                        }
                    }
                    if (flag)
                    {
                        he.DamageAmount = 0f;
                        if (s)
                        {
                            switch (num2)
                            {
                                case 0:
                                case 1:
                                case 2:
                                case 3:
                                case 4:
                                case 5:
                                case 6:
                                case 7:
                                case 8:
                                case 9:
                                case 10:
                                case 11:
                                case 12:
                                case 13:
                                case 14:
                                case 15:
                                    shotgwallhack[attacker.UID] = num2 + 1;
                                    return;
                            }
                        }
                        else
                        {
                            switch (num2)
                            {
                                case 0:
                                case 1:
                                case 2:
                                case 3:
                                case 4:
                                case 5:
                                    wallhack[attacker.UID] = num2 + 1;
                                    return;
                            }
                        }
                        wallhack.Remove(attacker.UID);
                        string line = DateTime.Now + " [SilentAim] Detected Silent Aim at " + attacker.Name + "(" + attacker.SteamID + ") , he tried to shoot " + vname + " through walls. From: " + attackerloc + " to " + lloc + " Ping: " + attacker.Ping;
                        Server.GetServer().BroadcastFrom("EAC", red + "Detected Silent Aim at " + attacker.Name);
                        file = new System.IO.StreamWriter(ppath, true);
                        file.WriteLine(line);
                        file.Close();
                        Warn(attacker, Penalities.SilentAim, true);
                    }
                    else if (num2 > 0)
                    {
                        if (s)
                        {
                            shotgwallhack[attacker.UID] = num2 - 1;
                        }
                        else
                        {
                            wallhack[attacker.UID] = num2 - 1;
                        }
                    }
                }
            }
            catch
            {
            }
        }



        public void OnCommand(Fougerite.Player pl, string cmd, string[] args)
        {
            if (cmd.Equals("drop"))
            {
                if (!pl.Admin && !pl.Moderator) return;
                if (args.Length == 0)
                {
                    return;
                }
                string s = string.Join(" ", args);
                Fougerite.Player fpl = Fougerite.Server.GetServer().FindPlayer(s);
                if (fpl == null) return;
                if (FlySuspect.ContainsKey(fpl.UID))
                {
                    FlySuspect.Remove(fpl.UID);
                }
                if (FlySuspectC.ContainsKey(fpl.UID))
                {
                    FlySuspectC.Remove(fpl.UID);
                }
                if (SustainedDetection.ContainsKey(fpl.UID))
                {
                    SustainedDetection.Remove(fpl.UID);
                }
            }
            else if (cmd.Equals("eac"))
            {
                if (args.Length == 0)
                {
                    pl.MessageFrom("EAC", green + "EAC " + yellow + " V" + Version + " [COLOR#FFFFFF] By DreTaX");
                    if (pl.Admin || pl.Moderator)
                    {
                        pl.MessageFrom("EAC", green + "/eac reload - Reloads EAC Config");
                        pl.MessageFrom("EAC", green + "/eac delwarns playername - Deletes warnings of a player");
                        pl.MessageFrom("EAC", green + "/eac delwarns steamid - Deletes warnings of an ID");
                        pl.MessageFrom("EAC", green + "/eac tpwarn - Teleports you to the next warned player.");
                        pl.MessageFrom("EAC", green + "/eac debug - Allows you to debug a few detections in EAC");
                    }
                }
                else
                {
                    if (pl.Admin || pl.Moderator)
                    {
                        string c = args[0];
                        var d = args.ToList();
                        d.Remove(args[0]);
                        string s = string.Join(" ", d.ToArray());
                        switch (c)
                        {
                            case "reload":
                                ReloadConfig();
                                pl.MessageFrom("EAC", "Reloaded!");
                                break;
                            case "tpwarn":
                                if (!NextWarned.Contains(pl.UID))
                                {
                                    NextWarned.Add(pl.UID);
                                    pl.MessageFrom("EAC", orange + "You will be teleported to the next warned player!");
                                }
                                else
                                {
                                    NextWarned.Remove(pl.UID);
                                    pl.MessageFrom("EAC", orange + "Disabled!");
                                }
                                break;
                            case "debug":
                                if (!Debug.Contains(pl.UID))
                                {
                                    Debug.Add(pl.UID);
                                    pl.MessageFrom("EAC", "Enabled!");
                                }
                                else
                                {
                                    Debug.Remove(pl.UID);
                                    pl.MessageFrom("EAC", "Disabled!");
                                }
                                break;
                            case "delwarns":
                                if (s.StartsWith("7656119"))
                                {
                                    foreach (var x in PenalityInis.Keys)
                                    {
                                        var inid = PenalityInis[x];
                                        if (inid.GetSetting("Warns", s) != null)
                                        {
                                            inid.DeleteSetting("Warns", s);
                                            inid.Save();
                                            pl.MessageFrom("EAC",
                                                yellow + s + green + " was deleted from the warn list!");
                                        }
                                        else
                                        {
                                            pl.MessageFrom("EAC", red + "Couldn't find " + s);
                                        }
                                    }
                                }
                                else
                                {
                                    Fougerite.Player p = Server.GetServer().FindPlayer(s);
                                    if (p != null)
                                    {
                                        foreach (var x in PenalityInis.Keys)
                                        {
                                            var inid = PenalityInis[x];
                                            inid.DeleteSetting("Warns", p.SteamID);
                                            inid.Save();
                                        }
                                        pl.MessageFrom("EAC", yellow + p.Name + green + " was deleted from the warn list!");
                                        p.MessageFrom("EAC", green + "Your warns were cleared!");
                                    }
                                    else
                                    {
                                        pl.MessageFrom("EAC", red + "Couldn't find " + s);
                                    }
                                }
                                break;
                            default:
                                pl.MessageFrom("EAC", "Command not found.");
                                break;
                        }
                    }
                }
            }
        }

        /*
         *
         * Hooks End
         *
         */

        /*
         *
         * EAC Checker
         *
         */

        public class EACChecker : MonoBehaviour
        {
            public float timeleft;
            public float lastTick;
            public float currentTick;
            public float deltaTime;
            public Vector3 lastPosition;
            public PlayerClient playerclient;
            public Character character;
            public Inventory inventory;
            public string userid;
            public float distance3D;
            public float distanceHeight;

            public float currentFloorHeight;
            public bool hasSearchedForFloor = false;

            public float lastSpeed = Time.realtimeSinceStartup;
            public int speednum = 0;


            public float lastWalkSpeed = Time.realtimeSinceStartup;
            public int walkspeednum = 0;
            public bool lastSprint = false;

            public float lastJump = Time.realtimeSinceStartup;
            public int jumpnum = 0;


            public float lastFly = Time.realtimeSinceStartup;
            public int flynum = 0;
            public Fougerite.Player FPlayer;

            public void Awake()
            {
                lastTick = Time.realtimeSinceStartup;
                enabled = false;
            }

            public void StartCheck()
            {
                this.playerclient = GetComponent<PlayerClient>();
                this.userid = this.playerclient.userID.ToString();
                FPlayer = Fougerite.Server.Cache[playerclient.userID];
                if (playerclient.controllable == null) return;
                this.character = playerclient.controllable.GetComponent<Character>();
                this.lastPosition = this.playerclient.lastKnownPosition;
                enabled = true;
            }
            public void FixedUpdate()
            {
                if (Time.realtimeSinceStartup - lastTick >= 1)
                {
                    currentTick = Time.realtimeSinceStartup;
                    deltaTime = currentTick - lastTick;
                    distance3D = Vector3.Distance(playerclient.lastKnownPosition, lastPosition) / deltaTime;
                    distanceHeight = (playerclient.lastKnownPosition.y - lastPosition.y) / deltaTime;
                    if (Speed) { checkSpeedhack(this); }
                    if (Walk) { checkWalkSpeedhack(this); }
                    lastPosition = playerclient.lastKnownPosition;
                    lastTick = currentTick;
                    this.hasSearchedForFloor = false;
                }
            }
            public Inventory GetInventory()
            {
                if (this.inventory == null) this.inventory = playerclient.rootControllable.idMain.GetComponent<Inventory>();
                return this.inventory;
            }

            public Character GetCharacter()
            {
                if (this.character == null) this.character = playerclient.rootControllable.idMain.GetComponent<Character>();
                return this.character;
            }

            public void OnDestroy()
            {
            }
        }

        public static void checkSpeedhack(EACChecker player)
        {
            float cdist = player.distance3D;
            Fougerite.Player p = Fougerite.Server.Cache[player.playerclient.userID];
            if (Math.Abs(player.distanceHeight) > speedDropIgnore)
            {
                player.speednum = 0;
                if (Debug.Contains(p.UID))
                {
                    p.MessageFrom("EAC DEBUG", "Distanceheight is bigger than speeddropignore. Height: " + player.distanceHeight + " | DropIgnore: " + speedDropIgnore);
                }
                return;
            }
            if ((double)Math.Round(cdist) <= (double)Math.Round(speedMinDistance))
            {
                player.speednum = 0;
                if (Debug.Contains(p.UID))
                {
                    p.MessageFrom("EAC DEBUG", "Distance is below mindistance. Dist-MinDist: " + (double)Math.Round(cdist) + " | " + (double)Math.Round(speedMinDistance));
                }
                return;
            }
            if (player.lastSpeed != player.lastTick)
            {
                player.speednum = 0;
                player.lastSpeed = player.currentTick;
                if (Debug.Contains(p.UID))
                {
                    p.MessageFrom("EAC DEBUG", "Last Speed doesn't equal with lasttick. Could be server lagg?");
                }
                return;
            }
            if (!p.IsOnline)
            {
                return;
            }
            string line;
            if (p.Ping >= PingToIgnore)
            {
                if (HighPings.Contains(p.UID)) return;
                line = DateTime.Now + " [SpeedHack] Detected Speedhack usage at " + p.Name + "(" + cdist + "m / s)" + " | " +
                    p.SteamID + " POSSIBLE LAGG, PING: " + p.Ping;
                MessageAdmins(yellow + p.Name + " might be using speedhack. Ping: " + p.Ping);
                file = new System.IO.StreamWriter(ppath, true);
                file.WriteLine(line);
                file.Close();
                HighPings.Add(p.UID);
                return;
            }
            if (cdist > speedMaxDistance)
            {
                if (HighPings.Contains(p.UID)) return;
                line = DateTime.Now + " [SpeedHack] Detected Speedhack usage at " + p.Name + "(" + cdist + "m / s)" + " | " +
                    p.SteamID + " POSSIBLE LAGG, Distance bigger than 25, Ping: " + p.Ping;
                MessageAdmins(yellow + p.Name + " might be using speedhack. Ping: " + p.Ping);
                file = new System.IO.StreamWriter(ppath, true);
                file.WriteLine(line);
                file.Close();
                HighPings.Add(p.UID);
                return;
            }
            player.speednum++;
            player.lastSpeed = player.currentTick;
            Server.GetServer().BroadcastFrom("EAC", red + "Detected Speedhack usage at " + p.Name + "(" + cdist + "m / s) Ping: " + p.Ping);
            line = DateTime.Now + " [SpeedHack] Detected Speedhack usage at " + p.Name + "(" + cdist + "m / s) Ping: " + p.Ping + " | " + p.SteamID;
            file = new System.IO.StreamWriter(ppath, true);
            file.WriteLine(line);
            file.Close();
            Warn(p, Penalities.Speed,true);
        }

        public static void checkWalkSpeedhack(EACChecker player)
        {
            float cdist = player.distance3D;
            if (player.character.stateFlags.sprint) { player.lastSprint = true; player.walkspeednum = 0; return; }
            if (player.distanceHeight < -walkspeedDropIgnore) { player.walkspeednum = 0; return; }
            if (cdist < walkspeedMinDistance) { player.walkspeednum = 0; return; }
            if (!player.character.stateFlags.grounded) { player.lastSprint = true; player.walkspeednum = 0; return; }
            if (player.lastSprint) { player.lastSprint = false; player.walkspeednum = 0; return; }
            if (player.lastWalkSpeed != player.lastTick) { player.walkspeednum = 0; player.lastWalkSpeed = player.currentTick; return; }

            player.walkspeednum++;
            player.lastWalkSpeed = player.currentTick;
            Fougerite.Player p = Fougerite.Server.Cache[player.playerclient.userID];
            if (!p.IsOnline)
            {
                return;
            }
            string line;
            if (p.Ping >= PingToIgnore)
            {
                if (HighPings.Contains(p.UID)) return;
                line = DateTime.Now + " [WalkSpeedHack] Detected Walk Speedhack usage at " + p.Name + "(" + cdist + "m / s)" + " | " +
                    p.SteamID + " POSSIBLE LAGG, PING: " + p.Ping;
                MessageAdmins(yellow + p.Name + " might be using walk speedhack. Ping: " + p.Ping);
                file = new System.IO.StreamWriter(ppath, true);
                file.WriteLine(line);
                file.Close();
                HighPings.Add(p.UID);
                return;
            }
            if (cdist > walkspeedMaxDistance)
            {
                if (HighPings.Contains(p.UID)) return;
                line = DateTime.Now + " [WalkSpeedHack] Detected Walk Speedhack usage at " + p.Name + "(" + cdist + "m / s)" + " | " +
                    p.SteamID + " POSSIBLE LAGG, PING: " + p.Ping;
                MessageAdmins(yellow + p.Name + " might be using walk speedhack. Ping: " + p.Ping);
                file = new System.IO.StreamWriter(ppath, true);
                file.WriteLine(line);
                file.Close();
                HighPings.Add(p.UID);
                return;
            }
            Server.GetServer().BroadcastFrom("EAC", red + "Detected Walk Speedhack usage at " + p.Name + "(" + cdist + "m / s) Ping: " + p.Ping);
            line = DateTime.Now + " [WalkSpeedHack] Detected Walk Speedhack usage at " + p.Name + "(" + cdist + "m / s) Ping: " + p.Ping + " | " + p.SteamID;
            file = new System.IO.StreamWriter(ppath, true);
            file.WriteLine(line);
            file.Close();
            Warn(p, Penalities.Speed, true);
        }

        /*
         *
         * EAC Checker End
         *
         */

        /*
         *
         * Other Methods
         *
         */

        private static bool PlayerHandlerHasGround(Vector3 pos)
        {
            float currentFloorHeight;
            RaycastHit r;
            if (Physics.Raycast(pos + UnderPlayerAdjustement, Vector3Down, out r, distanceDown))
            {
                currentFloorHeight = r.distance;
            }
            else
            {
                currentFloorHeight = 10f;
            }
            if (currentFloorHeight < 4f) return true;
            return false;
        }

        private static bool IsOnSupportPos(Vector3 v)
        {
            return Physics.OverlapSphere(v, 5f).Any(collider => collider.GetComponent<UnityEngine.MeshCollider>());
        }

        private static void MessageAdmins(string msg)
        {
            foreach (Fougerite.Player p in Fougerite.Server.GetServer().Players.Where(p => p.Admin || p.Moderator))
            {
                p.MessageFrom("EAC", msg);
            }
        }

        private static void Warn(Fougerite.Player p, Penalities data, bool kick = false)
        {
            var l = p.Location;
            int i = 1;
            if (SustainedDetection.ContainsKey(p.UID))
            {
                if (SustainedDetection[p.UID] >= 5)
                {
                    SustainedDetection.Remove(p.UID);
                }
            }
            var inidata = PenalityInis[data];
            if (inidata.GetSetting("Warns", p.SteamID) != null)
            {
                int.TryParse(inidata.GetSetting("Warns", p.SteamID), out i);
                i += 1;
            }
            else
            {
                inidata.AddSetting("Warns", p.SteamID, i.ToString());
            }
            bool ban = false;
            if (i >= PenalityWarns[data])
            {
                ban = true;
                Fougerite.Server.GetServer().BanPlayer(p, "EAC", "Exceeded the maximum warnings of " + data);
                inidata.DeleteSetting("Warns", p.SteamID);
            }
            else
            {
                inidata.SetSetting("Warns", p.SteamID, i.ToString());
            }
            inidata.Save();
            if (kick && !Debug.Contains(p.UID) && !ban)
            {
                if (p.IsOnline)
                {
                    p.Disconnect();
                }
            }
            foreach (var x in Fougerite.Server.GetServer().Players)
            {
                if (x.Admin || x.Moderator)
                {
                    if (NextWarned.Contains(x.UID))
                    {
                        NextWarned.Remove(x.UID);
                        x.TeleportTo(l, false);
                        x.MessageFrom("EAC", orange + " Teleported to the next warned player!");
                    }
                }
            }
            Server.GetServer().BroadcastFrom("EAC", red + "Warned " + p.Name + " Warnings [" + data + "] (" + i + "/" + PenalityWarns[data] + ")");
        }

        public EACTimedEvent CreateParallelTimer(int timeoutDelay, Dictionary<string, object> args)
        {
            EACTimedEvent timedEvent = new EACTimedEvent(timeoutDelay);
            timedEvent.Args = args;
            timedEvent.OnFire += Callback;
            return timedEvent;
        }

        private static void Callback(EACTimedEvent e)
        {
            var data = e.Args;
            e.Kill();
            if (data.ContainsKey("notif"))
            {
                Notified.Remove((ulong)data["notif"]);
                return;
            }
            Fougerite.Player pl = data["Player"] as Fougerite.Player;
            if (!pl.IsOnline)
            {
                return;
            }
            if (data.ContainsKey("kick"))
            {
                if (pl.IsOnline && !pl.IsDisconnecting)
                {
                    pl.Disconnect();
                }
                return;
            }
            if (data.ContainsKey("dizzy"))
            {
                var loc = DataStore.GetInstance().Get("EACDizzy", pl.UID);
                if (loc != null)
                {
                    var loc2 = (Vector3) loc;
                    if (loc2 != Vector3.zero)
                    {
                        var vdist = Vector3.Distance(loc2, pl.Location);
                        var dist = Math.Round(Math.Abs(loc2.y - pl.Location.y), 2);
                        if (dist >= 30 || vdist >= 26 || dist == 0)
                        {
                            return;
                        }
                        if (dist >= 2.50)
                        {
                            file2 = new System.IO.StreamWriter(ppath, true);
                            file2.WriteLine(DateTime.Now + " [Dizzy] " + pl.Name + "|" + pl.SteamID + " Player's location when launching timer: " + loc2 + " Loc Now: " + pl.Location + " Dist: " + dist);
                            int r = rnd.Next(1, 8156);
                            string l = cfg.GetSetting("DefaultLoc", r.ToString());
                            Vector3 v = Util.GetUtil().ConvertStringToVector3(l);
                            DataStore.GetInstance().Add("DizzySpawn", pl.UID, v);
                            pl.TeleportTo(v, false);
                            file2.WriteLine(DateTime.Now + " [Dizzy] " + pl.Name + "|" + pl.SteamID + " got dizzy warn!");
                            file2.Close();
                            Warn(pl, Penalities.Dizzy, true);
                        }
                    }
                }
            }
        }
    }
}
