using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using static ProtoBuf.IOEntity;

namespace Oxide.Plugins
{
    [Info("FlattenMap", "bmgjet", "1.0.1")]
    [Description("Flatten all BaseEntitys on the server into prefabs in the Map File.")]

    //Known Issues
    //IO Doesnt Copy (Complex IO, is being worked on)
    //Roofs Dont Shape when manually graded (need to use RE's SerialisedBlocks XML in MapData)
    //Leave Roofs unset so players can manually set in rust edit
    //Dont run on your live sever, Make a copy and run on a seperate one incase if fucked it some how.

    public class FlattenMap : RustPlugin
    {
        uint hazmat_suit = 199492240;
        uint scientistsuit = 1402387961;
        uint NPC_Spawner = 2359528520;
        List<PrefabData> MapPrefabs = new List<PrefabData>();
        List<IOEntity> FoundIO = new List<IOEntity>();
        List<SerializedIOEntity> IO = new List<SerializedIOEntity>();
        MapData OldXML;
        //Filter list to reduce unless prefabs being added making map too large to open.
        string[] NoCopy = new string[]
        {
            "assets/content/vehicles/", //vehicles
            "assets/prefabs/vehicle/", //Other vehicles
            "assets/bundled/prefabs/autospawn/collectable/", //Food collectables
            "assets/bundled/prefabs/autospawn/resource/", //Ores
            "assets/content/nature/plants", //Trees and bushes
            "assets/prefabs/weapons/", //Player usage weapons
            "assets/prefabs/weapon mods/", //Attachments
            "assets/prefabs/tools/", //Farming tools
            "assets/bundled/prefabs/system/", //Server Scripts
            "assets/prefabs/misc/burlap sack/generic_world.prefab", //Random item attached to some player
            "assets/rust.ai/", //Remove Bots
            "assets/prefabs/deployable/hot air balloon/subents/", //Remove sub entitys
            "assets/prefabs/misc/parachute/", //Remove parachute
            "assets/prefabs/player/", //Remove players
            "assets/prefabs/plants/", //Remove Plants
            "assets/prefabs/voiceaudio/boomboxportable/"
        };
        public static Dictionary<uint, string> Guns = new Dictionary<uint, string>
        {
            {1978739833,"rifle.ak"},
            {1665481300,"file.bolt"},
            {3474489095,"shotgun.double"},
            {2620171289,"rifle.l96"},
            {844375121,"rifle.lr300"},
            {1440914039,"rifle.m249"},
            {1517089664,"rifle.m39"},
            {2293870814,"pistol.m92"},
            {2545523575,"smg.mp5"},
            {4279856314,"pistol.nailgun"},
            {2696589892,"shotgun.waterpipe"},
            {3305012504,"pistol.python"},
            {2477536592,"pistol.revolver"},
            {554582418,"shotgun.pump"},
            {563371667,"pistol.semiauto"},
            {3759841439,"smg.2"},
            {1877401463,"shotgun.spas12"},
            {3243900999,"smg.thompson"}
        };
        //prefab id remapping
        Dictionary<uint, uint> twig = new Dictionary<uint, uint> {
            {2194854973,1093755168}, //Full Wall
            {310235277,2796204046}, //Low Wall
            {3531096400,591978447}, //Half Wall
            {919059809,3509200445}, //Wall Frame
            {803699375,1968164760}, //Doorway Wall
            {72949757,4249127830 }, //Foundation
            {916411076,2814800779}, //Floor
            {2326657495,1884291389}, //Window Wall
            {2925153068,2037725259}, //Floor Triangle
            {995542180,2701164785}, //Floor Triangle Frame
            {372561515,3786390809}, //Floor Frame
            //{3895720527,230496659}, //Roof
            {1961464529,202268266}, //Ushape Stair
            {2700861605,1111321898}, //Spiral Stair
            {3234260181,2918032421}, //Foundation triangle
            {623529040,3971126790}, //Ramp
            {1886694238,300409724}, //Foundation steps
            {447548514,836119247}, //Spiral Stair triangle
            {3250880722,2866578618}, //Stair Lshape
            //{870964632,4133535601} //Roof Triangle
             };
        Dictionary<uint, uint> wood = new Dictionary<uint, uint> {
            {2194854973,921983837},
            {310235277,2441851734},
            {3531096400,22851775},
            {919059809,3433239006},
            {803699375,1415488484},
            {72949757,3495715595},
            {916411076,3968278861},
            {2326657495,2885216655},
            {2925153068,2629308934},
            {995542180,2783850373},
            {372561515,190731310},
            //{3895720527,2150703424},
            {1961464529,3779627137},
            {2700861605,2557589201},
            {3234260181,2669235086},
            {623529040,1083816058},
            {1886694238,2317079308},
            {447548514,1226999144},
            {3250880722,2922770117},
            //{870964632,2650713663}
             };
        Dictionary<uint, uint> stone = new Dictionary<uint, uint> {
            {2194854973,3594132280},
            {310235277,2341413761},
            {3531096400,2246934718},
            {919059809,1218090580},
            {803699375,1169890993},
            {72949757,2880777998},
            {916411076,3859228539},
            {2326657495,759718278},
            {2925153068,648536205},
            {995542180,3024298727},
            {372561515,3939798636},
            //{3895720527,2720412746},
            {1961464529,888501382},
            {2700861605,2157709419},
            {3234260181,1914357817},
            {623529040,2227526408},
            {1886694238,2302430565},
            {447548514,2897638097},
            {3250880722,3400742811},
            //{870964632,300457304}
             };
        Dictionary<uint, uint> metal = new Dictionary<uint, uint> {
            {2194854973,3235325811},
            {310235277,325348053},
            {3531096400,3597441288},
            {919059809,4157340360},
            {803699375,1299162281},
            {72949757,3030821289},
            {916411076,119262343},
            {2326657495,46282902},
            {2925153068,770566494},
            {995542180,1579205971},
            {372561515,457145434},
            //{3895720527,2820366154},
            {1961464529,433379011},
            {2700861605,3595500402},
            {3234260181,3170121798},
            {623529040,2632011342},
            {1886694238,4183571989},
            {447548514,2752913795},
            {3250880722,2553053469},
            //{870964632,154365012}
            };
        Dictionary<uint, uint> hqm = new Dictionary<uint, uint> {
            {2194854973,787473322},
            {310235277,46025092},
            {3531096400,1924332768},
            {919059809,2342472642},
            {803699375,1161962274},
            {72949757,3929334978},
            {916411076,2946948004},
            {2326657495,16915376},
            {2925153068,2943973556},
            {995542180,468444903},
            {372561515,1169583692},
            //{3895720527,3900016524},
            {1961464529,585390072},
            {2700861605,1430375591},
            {3234260181,977022139},
            {623529040,4170688610},
            {1886694238,2826341200},
            {447548514,2729607885},
            {3250880722,355230703},
            //{870964632,4060621656}
             };

        public class SerializedIOEntity
        {
            public SerializedIOEntity()
            {
            }
            public SerializedIOEntity(IOEntity _io)
            {
                this.fullPath = _io.PrefabName;
                this.position = _io.transform.position;

                List<SerializedConnectionData> IOin = new List<SerializedConnectionData>();
                for (int i = 0; i < _io.inputs.Length; i++)
                {

                    SerializedConnectionData entry = new SerializedConnectionData();
                    entry.connectedTo = 0;// (int)_io.inputs[i].connectedTo.entityRef.uid;
                    BaseEntity master = _io.inputs[i].connectedTo.entityRef.Get(true);
                    if (master != null)
                    {
                        entry.fullPath = master.PrefabName;
                        entry.position = master.transform.position;
                        entry.type = 0;
                        entry.input = true;
                        IOin.Add(entry);
                    }
                }
                this.inputs = IOin.ToArray();

                List<SerializedConnectionData> IOout = new List<SerializedConnectionData>();
                for (int i = 0; i < _io.outputs.Length; i++)
                {
                    try
                    {
                        SerializedConnectionData entry = new SerializedConnectionData();
                        entry.connectedTo = (int)_io.outputs[i].connectedTo.entityRef.uid;
                        entry.fullPath = _io.outputs[i].connectedTo.ioEnt.PrefabName;
                        entry.position = _io.outputs[i].connectedTo.ioEnt.transform.position;
                        entry.type = 0;
                        entry.input = false;
                        IOout.Add(entry);
                    }
                    catch { }
                }
                this.outputs = IOout.ToArray();
                this.accessLevel = 0;
                this.doorEffect = -1;
                this.timerLength = ((_io is TimerSwitch) ? (_io as TimerSwitch).timerLength : ((_io is CardReader) ? (_io as CardReader).accessDuration : 0f));
                this.frequency = ((_io is RFBroadcaster) ? (_io as RFBroadcaster).frequency : ((_io is RFReceiver) ? (_io as RFReceiver).frequency : 0));
                this.unlimitedAmmo = false;
                this.peaceKeeper = (_io is AutoTurret && (_io as AutoTurret).PeacekeeperMode());
                if (_io is AutoTurret)
                {
                    this.autoTurretWeapon = FlattenMap.GetGun(_io);
                }
                this.branchAmount = ((_io is ElectricalBranch) ? (_io as ElectricalBranch).branchAmount : 0);
                this.targetCounterNumber = ((_io is PowerCounter) ? (_io as PowerCounter).targetCounterNumber : 0);
                this.counterPassthrough = (_io is PowerCounter && (_io as PowerCounter).DisplayPassthrough());
                this.rcIdentifier = ((_io is CCTV_RC) ? (_io as CCTV_RC).rcIdentifier : string.Empty);
                this.floors = ((_io is Elevator) ? (_io as Elevator).Floor : 1);
                this.phoneName = ((_io is Telephone) ? (_io as Telephone)._name : string.Empty);
            }
            public string fullPath;
            public VectorData position;
            public SerializedConnectionData[] inputs;
            public SerializedConnectionData[] outputs;
            public int accessLevel;
            public int doorEffect;
            public float timerLength;
            public int frequency;
            public bool unlimitedAmmo;
            public bool peaceKeeper;
            public string autoTurretWeapon;
            public int branchAmount;
            public int targetCounterNumber;
            public string rcIdentifier;
            public bool counterPassthrough;
            public int floors = 1;
            public string phoneName;
        }
        public class SerializedConnectionData
        {
            public SerializedConnectionData()
            {
            }
            public SerializedConnectionData(IOEntity _IO, bool _input, int _connectedto, int _type)
            {
                this.fullPath = _IO.gameObject.name;
                this.position = (_IO.transform.position);
                this.input = _input;
                this.connectedTo = _connectedto;
                this.type = _type;
            }
            public string fullPath;
            public VectorData position;
            public bool input;
            public int connectedTo;
            public int type;
        }

        //Main flatten code
        uint flatten(string name, bool ClearFirst = false, bool upgradeparts = true, bool filter = true, bool filterbaseplayers = false, bool FilterZero = true, bool FilterLocks = true, bool replaceplayers = true, bool runio = true)
        {
            MapPrefabs.Clear();
            FoundIO.Clear();
            IO.Clear();
            OldXML = null;
            //flattenmap nztest true true false true true true
            uint dupes = 0;
            bool flag = false;
            MapPrefabs = World.Serialization.world.prefabs;
            //Extract Map Data to find SerializedIOData
            string EncodedIOName = MapDataName(World.Serialization.world.prefabs.Count);
            for (int i = World.Serialization.world.maps.Count - 1; i >= 0; i--)
            {
                MapData mapdata = World.Serialization.world.maps[i];
                if (mapdata.name == EncodedIOName)
                {
                    Puts("Found Rust Edit IO Data");
                    OldXML = mapdata;
                }
            }
            if (ClearFirst)
            {
                Puts("Clearing Maps Prefabs list");
                World.Serialization.world.prefabs.Clear();
            }
            //Loop though each BaseEntity
            foreach (BaseEntity _baseentity in BaseEntity.serverEntities)
            {
                //Filter out null/invalid stuff
                if (_baseentity.prefabID == 0 || _baseentity.transform.position == null || _baseentity.transform.rotation == null || _baseentity.transform.localScale == null) { continue; }
                if (FilterZero && _baseentity.transform.position == new Vector3(0f, 0f, 0f))
                {
                    //Most likely a server script or junk collection
                    dupes++;
                    continue;
                }
                //Filter out locks
                if (FilterLocks && (_baseentity is KeyLock || _baseentity is CodeLock))
                {
                    dupes++;
                    continue;
                }
                //filter out stuff that break map file
                if (_baseentity is Elevator || _baseentity is NeonSign)

                    //Filter out baseplayer/npcs
                    if (filterbaseplayers && (_baseentity.ToPlayer() != null || _baseentity.IsNpc == true)) { continue; }
                if (filter)
                {
                    flag = false;
                    foreach (string s in NoCopy)
                    {
                        //Filter out from filter list
                        if (_baseentity.PrefabName.Contains(s))
                        {
                            dupes++;
                            flag = true;
                            break;
                        }
                    }
                }
                if (flag) { continue; }
                //Create prefab data using current BaseEntity
                PrefabData prefabdatatest = new PrefabData();
                prefabdatatest.category = "Decor";
                prefabdatatest.position = _baseentity.transform.position;
                prefabdatatest.rotation = _baseentity.transform.rotation;
                prefabdatatest.scale = _baseentity.transform.localScale;
                prefabdatatest.id = _baseentity.prefabID;
                prefabdatatest.ShouldPool = true;

                //Replaces baseplayers with hazmat or scientistsuit for npcs
                if (replaceplayers && _baseentity.ToPlayer() != null)
                {
                    if (_baseentity.ToPlayer().IsNpc)
                    {
                        prefabdatatest.id = NPC_Spawner;//scientistsuit
                    }
                    else
                    {
                        prefabdatatest.id = hazmat_suit;
                    }
                }
                //Dupe Test
                if (World.Serialization.world.prefabs.Contains(prefabdatatest))
                {
                    dupes++;
                    continue;
                }
                //Replaces Player Buildings With Server Parts
                BuildingBlock bb = _baseentity as BuildingBlock;
                if (bb != null && upgradeparts)
                {
                    prefabdatatest = BuildBlocks(bb, prefabdatatest);
                }
                IOEntity io = _baseentity as IOEntity;
                if (io != null && upgradeparts)
                {
                    FoundIO.Add(io);
                }
                //Adds prefab to the map file.
                World.Serialization.world.prefabs.Add(prefabdatatest);
            }

            if (runio)
            {
                //Does the IO stuff
                DoSerializedIOEntity();
            }

            //Saves Map File
            Puts("Prefabs Added To Map " + World.Serialization.world.prefabs.Count);
            World.Serialization.Save(name + ".map");
            World.Serialization.world.prefabs.Clear();
            World.Serialization.world.prefabs = MapPrefabs;
            return dupes;
        }

        void DoSerializedIOEntity()
        {
            Puts("Found " + FoundIO.Count.ToString() + " IO Entitys");

            foreach (IOEntity _io in FoundIO)
            {
                //Convert IOEntity into SerializedIOEntity (Rust Edit Format)
                try
                {
                    SerializedIOEntity serializedIOEntity = new SerializedIOEntity(_io);
                    IO.Add(serializedIOEntity);
                }
                catch
                {
                    Puts(_io.ShortPrefabName);
                    Puts("Bad IO");
                }
            }

            if (OldXML != null)
            {
                Puts("Editing SerializedIOData");
                string name = MapDataName(World.Serialization.world.prefabs.Count);
                World.Serialization.GetMap(OldXML.name).name = name; //Update New Name
                if (IO.Count != 0)
                {
                    //Read out old data and makes changes only if there IO to add
                    ReadOldXML(Encoding.ASCII.GetString(World.Serialization.GetMap(OldXML.name).data));
                    //Update with new XML
                    World.Serialization.GetMap(name).data = Encoding.ASCII.GetBytes(CreateNewXML());
                    Puts("SerializedIOEntity Updated");
                    return;
                }
                Puts("SerializedIOEntity Renamed");
            }
            else
            {
                if (IO.Count != 0)
                {
                    Puts("Creating New SerializedIOData");
                    World.Serialization.AddMap(MapDataName(World.Serialization.world.prefabs.Count), Encoding.ASCII.GetBytes(CreateNewXML()));
                    Puts("SerializedIOEntity Done");
                }
            }

        }

        static string GetGun(IOEntity _io)
        {
            try
            {
                BaseProjectile gun = (_io as AutoTurret).GetAttachedWeapon();
                if (gun != null)
                {
                    return Guns[gun.prefabID];
                }
            }
            catch { Console.WriteLine("Turret ( " + _io.transform.position + " ): Not a rust edit supported gun"); }
            return string.Empty;
        }
        void ReadOldXML(string XML)
        {
            List<SerializedIOEntity> IO = new List<SerializedIOEntity>();
            string[] info = XML.Split(new string[] { "<SerializedIOEntity>" }, StringSplitOptions.None);
            foreach (string decode in info)
            {
                if (decode.Contains("<entities>")) continue;
                SerializedIOEntity RustEdit = new SerializedIOEntity();
                RustEdit.fullPath = decode.Split(new string[] { "<fullPath>" }, StringSplitOptions.None)[1].Split(new string[] { "</fullPath>" }, StringSplitOptions.None)[0];
                //Get Position
                string pos = decode.Split(new string[] { "<position>" }, StringSplitOptions.None)[1].Split(new string[] { "</position>" }, StringSplitOptions.None)[0];
                RustEdit.position.x = float.Parse(pos.Split(new string[] { "<x>" }, StringSplitOptions.None)[1].Split(new string[] { "</x>" }, StringSplitOptions.None)[0]);
                RustEdit.position.y = float.Parse(pos.Split(new string[] { "<y>" }, StringSplitOptions.None)[1].Split(new string[] { "</y>" }, StringSplitOptions.None)[0]);
                RustEdit.position.z = float.Parse(pos.Split(new string[] { "<z>" }, StringSplitOptions.None)[1].Split(new string[] { "</z>" }, StringSplitOptions.None)[0]);
                try
                {
                    string[] ins = decode.Split(new string[] { "<inputs>" }, StringSplitOptions.None)[1].Split(new string[] { "</inputs>" }, StringSplitOptions.None)[0].Split(new string[] { "<SerializedConnectionData>" }, StringSplitOptions.None);
                    List<SerializedConnectionData> connections = new List<SerializedConnectionData>();
                    foreach (string _in in ins)
                    {
                        if (_in.Contains("</SerializedConnectionData>"))
                        {
                            SerializedConnectionData cd = new SerializedConnectionData();
                            cd.fullPath = _in.Split(new string[] { "<fullPath>" }, StringSplitOptions.None)[1].Split(new string[] { "</fullPath>" }, StringSplitOptions.None)[0];
                            string pos2 = _in.Split(new string[] { "<position>" }, StringSplitOptions.None)[1].Split(new string[] { "</position>" }, StringSplitOptions.None)[0];
                            cd.position.x = float.Parse(pos2.Split(new string[] { "<x>" }, StringSplitOptions.None)[1].Split(new string[] { "</x>" }, StringSplitOptions.None)[0]);
                            cd.position.y = float.Parse(pos2.Split(new string[] { "<y>" }, StringSplitOptions.None)[1].Split(new string[] { "</y>" }, StringSplitOptions.None)[0]);
                            cd.position.z = float.Parse(pos2.Split(new string[] { "<z>" }, StringSplitOptions.None)[1].Split(new string[] { "</z>" }, StringSplitOptions.None)[0]);
                            cd.input = bool.Parse(_in.Split(new string[] { "<input>" }, StringSplitOptions.None)[1].Split(new string[] { "</input>" }, StringSplitOptions.None)[0]);
                            cd.connectedTo = int.Parse(_in.Split(new string[] { "<connectedTo>" }, StringSplitOptions.None)[1].Split(new string[] { "</connectedTo>" }, StringSplitOptions.None)[0]);
                            cd.fullPath = _in.Split(new string[] { "<fullPath>" }, StringSplitOptions.None)[1].Split(new string[] { "</fullPath>" }, StringSplitOptions.None)[0];
                            connections.Add(cd);
                        }
                    }
                    RustEdit.inputs = connections.ToArray();
                }
                catch
                { //No Inputs Found
                }
                try
                {
                    string[] outs = decode.Split(new string[] { "<outputs>" }, StringSplitOptions.None)[1].Split(new string[] { "</outputs>" }, StringSplitOptions.None)[0].Split(new string[] { "<SerializedConnectionData>" }, StringSplitOptions.None);
                    List<SerializedConnectionData> connections = new List<SerializedConnectionData>();
                    foreach (string _out in outs)
                    {
                        if (_out.Contains("</SerializedConnectionData>"))
                        {
                            SerializedConnectionData cd = new SerializedConnectionData();
                            cd.fullPath = _out.Split(new string[] { "<fullPath>" }, StringSplitOptions.None)[1].Split(new string[] { "</fullPath>" }, StringSplitOptions.None)[0];
                            string pos2 = _out.Split(new string[] { "<position>" }, StringSplitOptions.None)[1].Split(new string[] { "</position>" }, StringSplitOptions.None)[0];
                            cd.position.x = float.Parse(pos2.Split(new string[] { "<x>" }, StringSplitOptions.None)[1].Split(new string[] { "</x>" }, StringSplitOptions.None)[0]);
                            cd.position.y = float.Parse(pos2.Split(new string[] { "<y>" }, StringSplitOptions.None)[1].Split(new string[] { "</y>" }, StringSplitOptions.None)[0]);
                            cd.position.z = float.Parse(pos2.Split(new string[] { "<z>" }, StringSplitOptions.None)[1].Split(new string[] { "</z>" }, StringSplitOptions.None)[0]);
                            cd.input = bool.Parse(_out.Split(new string[] { "<input>" }, StringSplitOptions.None)[1].Split(new string[] { "</input>" }, StringSplitOptions.None)[0]);
                            cd.connectedTo = int.Parse(_out.Split(new string[] { "<connectedTo>" }, StringSplitOptions.None)[1].Split(new string[] { "</connectedTo>" }, StringSplitOptions.None)[0]);
                            cd.fullPath = _out.Split(new string[] { "<fullPath>" }, StringSplitOptions.None)[1].Split(new string[] { "</fullPath>" }, StringSplitOptions.None)[0];
                            connections.Add(cd);
                        }
                    }
                    RustEdit.outputs = connections.ToArray();
                }
                catch
                { //No Outputs Found
                }
                try
                {
                    RustEdit.accessLevel = int.Parse(decode.Split(new string[] { "<accessLevel>" }, StringSplitOptions.None)[1].Split(new string[] { "</accessLevel>" }, StringSplitOptions.None)[0]);
                }
                catch { }
                try
                {
                    RustEdit.doorEffect = int.Parse(decode.Split(new string[] { "<doorEffect>" }, StringSplitOptions.None)[1].Split(new string[] { "</doorEffect>" }, StringSplitOptions.None)[0]);
                }
                catch { }
                try
                {
                    RustEdit.timerLength = int.Parse(decode.Split(new string[] { "<timerLength>" }, StringSplitOptions.None)[1].Split(new string[] { "</timerLength>" }, StringSplitOptions.None)[0]);
                }
                catch { }
                try
                {
                    RustEdit.frequency = int.Parse(decode.Split(new string[] { "<frequency>" }, StringSplitOptions.None)[1].Split(new string[] { "</frequency>" }, StringSplitOptions.None)[0]);
                }
                catch { }
                try
                {
                    RustEdit.unlimitedAmmo = bool.Parse(decode.Split(new string[] { "<unlimitedAmmo>" }, StringSplitOptions.None)[1].Split(new string[] { "</unlimitedAmmo>" }, StringSplitOptions.None)[0]);
                }
                catch { }
                try
                {
                    RustEdit.peaceKeeper = bool.Parse(decode.Split(new string[] { "<peaceKeeper>" }, StringSplitOptions.None)[1].Split(new string[] { "</peaceKeeper>" }, StringSplitOptions.None)[0]);
                }
                catch { }
                try
                {
                    RustEdit.autoTurretWeapon = (decode.Split(new string[] { "<autoTurretWeapon>" }, StringSplitOptions.None)[1].Split(new string[] { "</autoTurretWeapon>" }, StringSplitOptions.None)[0]);

                }
                catch { }
                try
                {
                    RustEdit.branchAmount = int.Parse(decode.Split(new string[] { "<branchAmount>" }, StringSplitOptions.None)[1].Split(new string[] { "</branchAmount>" }, StringSplitOptions.None)[0]);
                }
                catch { }
                try
                {
                    RustEdit.targetCounterNumber = int.Parse(decode.Split(new string[] { "<targetCounterNumber>" }, StringSplitOptions.None)[1].Split(new string[] { "</targetCounterNumber>" }, StringSplitOptions.None)[0]);
                }
                catch { }
                try
                {
                    RustEdit.rcIdentifier = decode.Split(new string[] { "<rcIdentifier>" }, StringSplitOptions.None)[1].Split(new string[] { "</rcIdentifier>" }, StringSplitOptions.None)[0];
                }
                catch { }
                try
                {
                    RustEdit.counterPassthrough = bool.Parse(decode.Split(new string[] { "<counterPassthrough>" }, StringSplitOptions.None)[1].Split(new string[] { "</counterPassthrough>" }, StringSplitOptions.None)[0]);
                }
                catch { }
                try
                {
                    RustEdit.floors = int.Parse(decode.Split(new string[] { "<floors>" }, StringSplitOptions.None)[1].Split(new string[] { "</floors>" }, StringSplitOptions.None)[0]);
                }
                catch { }
                try
                {
                    RustEdit.phoneName = decode.Split(new string[] { "<phoneName>" }, StringSplitOptions.None)[1].Split(new string[] { "</phoneName>" }, StringSplitOptions.None)[0];
                }
                catch { }
                IO.Add(RustEdit);
            }
        }

        string CreateNewXML()
        {
            //Create XML File
            string NewXML = Encoding.ASCII.GetString(Convert.FromBase64String("PD94bWwgdmVyc2lvbj0iMS4wIj8+PFNlcmlhbGl6ZWRJT0RhdGEgeG1sbnM6eHNkPSJodHRwOi8vd3d3LnczLm9yZy8yMDAxL1hNTFNjaGVtYSIgeG1sbnM6eHNpPSJodHRwOi8vd3d3LnczLm9yZy8yMDAxL1hNTFNjaGVtYS1pbnN0YW5jZSI+PGVudGl0aWVzPg=="));
            foreach (SerializedIOEntity io in IO)
            {
                string newentity = "<SerializedIOEntity>" +
                    "<fullPath>" + io.fullPath + "</fullPath>" +
                    "<position>" +
                    "<x>" + io.position.x + "</x>" +
                    "<y>" + io.position.y + "</y>" +
                    "<z>" + io.position.z + "</z>" +
                    "</position>";
                if (io.inputs.Length == 0)
                {
                    newentity += "<inputs />";
                }
                else
                {
                    foreach (SerializedConnectionData scd in io.inputs)
                    {
                        newentity +=
                        "<inputs>" +
                        "<SerializedConnectionData>" +
                        "<fullPath>" + scd.fullPath + "</fullPath>" +
                        "<position>" +
                        "<x>" + scd.position.x + "</x>" +
                        "<y>" + scd.position.y + "</y>" +
                        "<z>" + scd.position.z + "</z>" +
                        "</position>" +
                        "<input>" + scd.input.ToString().ToLower() + "</input>" +
                        "<connectedTo>" + "0" + "</connectedTo>" + //"<connectedTo>" + scd.connectedTo + "</connectedTo>" +
                        "<type>" + scd.type + "</type>" +
                        "</SerializedConnectionData>" +
                        "</inputs>";
                    }
                }
                if (io.outputs.Length == 0)
                {
                    newentity += "<outputs ><SerializedConnectionData xsi:nil=\"true\" /></outputs>";
                }
                else
                {
                    foreach (SerializedConnectionData scd in io.outputs)
                    {
                        newentity +=
                        "<outputs>" +
                        "<SerializedConnectionData>" +
                        "<fullPath>" + scd.fullPath + "</fullPath>" +
                        "<position>" +
                        "<x>" + scd.position.x + "</x>" +
                        "<y>" + scd.position.y + "</y>" +
                        "<z>" + scd.position.z + "</z>" +
                        "</position>" +
                        "<input>" + scd.input.ToString().ToLower() + "</input>" +
                        "<connectedTo>" + "0" + "</connectedTo>" + //"<connectedTo>" + scd.connectedTo + "</connectedTo>" +
                        "<type>" + scd.type + "</type>" +
                        "</SerializedConnectionData>" +
                        "</outputs>";
                    }
                }
                newentity +=
                    "<accessLevel>" + io.accessLevel + "</accessLevel>" +
                    "<doorEffect>" + io.doorEffect + "</doorEffect>" +
                    "<timerLength>" + io.timerLength + "</timerLength>" +
                    "<frequency>" + io.frequency + "</frequency>" +
                    "<unlimitedAmmo>" + io.unlimitedAmmo.ToString().ToLower() + "</unlimitedAmmo>" +
                    "<peaceKeeper>" + io.peaceKeeper.ToString().ToLower() + "</peaceKeeper>" +
                    "<autoTurretWeapon>" + io.autoTurretWeapon + "</autoTurretWeapon>" +
                    "<branchAmount>" + io.branchAmount + "</branchAmount>" +
                    "<targetCounterNumber>" + io.targetCounterNumber + "</targetCounterNumber>" +
                    "<rcIdentifier>" + io.rcIdentifier + "</rcIdentifier>" +
                    "<counterPassthrough>" + io.counterPassthrough.ToString().ToLower() + "</counterPassthrough>" +
                    "<floors>" + io.floors + "</floors>" +
                    "<phoneName>" + io.phoneName + "</phoneName>" +
                    "</SerializedIOEntity>";

                NewXML += newentity;
            }
            //CleanUp
            NewXML = NewXML.Replace("<phoneName></phoneName>", "<phoneName />").Replace("<rcIdentifier></rcIdentifier>", "<rcIdentifier />").Replace("<autoTurretWeapon></autoTurretWeapon>", "<autoTurretWeapon />").Replace("<outputs></outputs>", "<outputs><SerializedConnectionData xsi:nil=\"true\" /></outputs>");
            NewXML += "</entities></SerializedIOData>";
            return NewXML;
        }

        PrefabData BuildBlocks(BuildingBlock _baseentity, PrefabData prefabdatatest)
        {
            switch (_baseentity.grade)
            {
                case (BuildingGrade.Enum)0:
                    prefabdatatest.id = upgrade(0, _baseentity.prefabID);
                    //twig
                    break;
                case (BuildingGrade.Enum)1:
                    prefabdatatest.id = upgrade(1, _baseentity.prefabID);
                    //wood
                    break;
                case (BuildingGrade.Enum)2:
                    prefabdatatest.id = upgrade(2, _baseentity.prefabID);
                    //stone
                    break;
                case (BuildingGrade.Enum)3:
                    prefabdatatest.id = upgrade(3, _baseentity.prefabID);
                    //metal
                    break;
                case (BuildingGrade.Enum)4:
                    prefabdatatest.id = upgrade(4, _baseentity.prefabID);
                    //hqm
                    break;
            }
            //FoundationFix (Adds the 4 sides of the foundation)
            switch (prefabdatatest.id)
            {
                case 2880777998:
                    //stone square
                    foundationfix(_baseentity, 3157545414, -3f);
                    break;
                case 3030821289:
                    //metal square
                    foundationfix(_baseentity, 2707660104, -3f);
                    break;
                case 3929334978:
                    //hqm square
                    foundationfix(_baseentity, 729368649);
                    break;
                case 1914357817:
                    //stone triangle
                    foundationfix(_baseentity, 3157545414, -3f, true);
                    break;
                case 3170121798:
                    //metal triangle
                    foundationfix(_baseentity, 2707660104, -3f, true);
                    break;
                case 977022139:
                    //hqm triangle
                    foundationfix(_baseentity, 729368649, 0f, true);
                    break;
            }
            //RoofFix (Adds roof top and bottom liners)
            switch (prefabdatatest.id)
            {
                case 230496659:
                    //twig square
                    rooffix(_baseentity, 1064409401, 1564525410);
                    break;
                case 2150703424:
                    //wood square
                    rooffix(_baseentity, 2999729001, 1599154272);
                    break;
                case 2720412746:
                    //stone square
                    rooffix(_baseentity, 2950685079, 86817151);
                    break;
                case 2820366154:
                    //metal square
                    rooffix(_baseentity, 657499023, 153260206);
                    break;
                case 3900016524:
                    //hqm square
                    rooffix(_baseentity, 3471256028, 1995132703);
                    break;
            }
            return prefabdatatest;
        }

        void foundationfix(BuildingBlock buildingblock, uint id, float offset = 0f, bool triangle = false)
        {
            //Adds the missing parts
            if (triangle)
            {
                World.Serialization.AddPrefab("Decor", id, buildingblock.transform.position + buildingblock.transform.TransformDirection(new Vector3(0, offset, 0f)), buildingblock.transform.rotation * Quaternion.Euler(0, 90, 0), new Vector3(1, 1, 1));
                World.Serialization.AddPrefab("Decor", id, buildingblock.transform.position + buildingblock.transform.TransformDirection(new Vector3(0.733f, offset, 1.354f)), buildingblock.transform.rotation * Quaternion.Euler(0, -30, 0), new Vector3(1, 1, 1));
                World.Serialization.AddPrefab("Decor", id, buildingblock.transform.position + buildingblock.transform.TransformDirection(new Vector3(-0.733f, offset, 1.354f)), buildingblock.transform.rotation * Quaternion.Euler(0, 210, 0), new Vector3(1, 1, 1));
                return;
            }
            World.Serialization.AddPrefab("Decor", id, buildingblock.transform.position + buildingblock.transform.TransformDirection(new Vector3(1.5f, offset, 0)), buildingblock.transform.rotation, new Vector3(1, 1, 1));
            World.Serialization.AddPrefab("Decor", id, buildingblock.transform.position + buildingblock.transform.TransformDirection(new Vector3(-1.5f, offset, 0)), buildingblock.transform.rotation * Quaternion.Euler(0, 180, 0), new Vector3(1, 1, 1));
            World.Serialization.AddPrefab("Decor", id, buildingblock.transform.position + buildingblock.transform.TransformDirection(new Vector3(0, offset, 1.5f)), buildingblock.transform.rotation * Quaternion.Euler(0, 270, 0), new Vector3(1, 1, 1));
            World.Serialization.AddPrefab("Decor", id, buildingblock.transform.position + buildingblock.transform.TransformDirection(new Vector3(0, offset, -1.5f)), buildingblock.transform.rotation * Quaternion.Euler(0, 90, 0), new Vector3(1, 1, 1));
        }
        void rooffix(BuildingBlock buildingblock, uint top, uint bottom)
        {
            //Adds the 2 missing parts
            World.Serialization.AddPrefab("Decor", top, buildingblock.transform.position, buildingblock.transform.rotation, new Vector3(1, 1, 1));
            World.Serialization.AddPrefab("Decor", bottom, buildingblock.transform.position, buildingblock.transform.rotation, new Vector3(1, 1, 1));
        }

        uint upgrade(int grade, uint prefab)
        {
            //try catch so if something unknown is passed it makes no prefab changes
            try
            {
                switch (grade)
                {
                    case 0:
                        prefab = twig[prefab];
                        break;
                    case 1:
                        prefab = wood[prefab];
                        break;
                    case 2:
                        prefab = stone[prefab];
                        break;
                    case 3:
                        prefab = metal[prefab];
                        break;
                    case 4:
                        prefab = hqm[prefab];
                        break;
                }
            }
            catch { }
            return prefab;
        }

        int setting(string[] Args)
        {
            switch (Args.Length)
            {
                case 1:
                    return (int)flatten(Args[0]); //Default
                case 2:
                    return (int)flatten(Args[0], bool.Parse(Args[1])); //Wipe maps prefabs to use BaseEntitys only.
                case 3:
                    return (int)flatten(Args[0], bool.Parse(Args[1]), bool.Parse(Args[2])); //upgrade player bases to server parts
                case 4:
                    return (int)flatten(Args[0], bool.Parse(Args[1]), bool.Parse(Args[2]), bool.Parse(Args[3])); //upgrade player bases to server parts
                case 5:
                    return (int)flatten(Args[0], bool.Parse(Args[1]), bool.Parse(Args[2]), bool.Parse(Args[3]), bool.Parse(Args[4])); //Filter baseplayer/npc
                case 6:
                    return (int)flatten(Args[0], bool.Parse(Args[1]), bool.Parse(Args[2]), bool.Parse(Args[3]), bool.Parse(Args[4]), bool.Parse(Args[5])); //Filter zero position
                case 7:
                    return (int)flatten(Args[0], bool.Parse(Args[1]), bool.Parse(Args[2]), bool.Parse(Args[3]), bool.Parse(Args[4]), bool.Parse(Args[5]), bool.Parse(Args[6])); //Filter key/code locks
                case 8:
                    return (int)flatten(Args[0], bool.Parse(Args[1]), bool.Parse(Args[2]), bool.Parse(Args[3]), bool.Parse(Args[4]), bool.Parse(Args[5]), bool.Parse(Args[6]), bool.Parse(Args[7])); //Replace base players
                case 9:
                    return (int)flatten(Args[0], bool.Parse(Args[1]), bool.Parse(Args[2]), bool.Parse(Args[3]), bool.Parse(Args[4]), bool.Parse(Args[5]), bool.Parse(Args[6]), bool.Parse(Args[7]), bool.Parse(Args[8])); //try process IO data
                default:
                    return -1;
            }
        }

        string HelpMessage()
        {
            return "Help:\r\n/flatten mapname\r\n---Optional args after mapname---\r\ntrue/fase wipe maps prefabs and use BaseEntitys Only\r\ntrue/false upgrade player bases to server parts\r\ntrue/false apply filter list\r\ntrue/false apply baseplayer filter\r\ntrue/false filter prefabs at location 0,0,0\r\ntrue/false filter key/code locks\r\ntrue/false replace baseplayers with hazmat (must have baseplayer filter off\r\n";
        }
        public static string MapDataName(int PreFabCount)
        {
            using (Aes aes = Aes.Create())
            {
                Rfc2898DeriveBytes rfc2898DeriveBytes = new Rfc2898DeriveBytes(PreFabCount.ToString(), new byte[] { 73, 118, 97, 110, 32, 77, 101, 100, 118, 101, 100, 101, 118 });
                aes.Key = rfc2898DeriveBytes.GetBytes(32);
                aes.IV = rfc2898DeriveBytes.GetBytes(16);
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        byte[] Hashed = Encoding.Unicode.GetBytes("ioentitydata");
                        cryptoStream.Write(Hashed, 0, Hashed.Length);
                        cryptoStream.Close();
                    }
                    return Convert.ToBase64String(memoryStream.ToArray());
                }
            }
        }

        private void Unload()
        {
            if (Guns != null) Guns = null;
        }

        [ConsoleCommand("flattenmap")]
        private void Consoleflatten(ConsoleSystem.Arg arg)
        {
            if (!arg.IsAdmin) return;
            Puts("Flattening Map, May take some time on high ent slow servers!");
            if (arg.Args == null)
            {
                Puts(HelpMessage());
                return;
            }
            int dupes = setting(arg.Args);
            if (dupes == -1)
            {
                Puts(HelpMessage());
                return;
            }
            Puts("Saved Map " + dupes.ToString() + " prefabs ignored");
        }

        [ChatCommand("flatten")]
        private void Cmdflatten(BasePlayer player, string command, string[] args)
        {
            if (player == null || !player.IsAdmin) return;
            if (args == null)
            {
                player.ChatMessage(HelpMessage());
                return;
            }
            player.ChatMessage("Flattening Map, May take some time on high ent slow servers!");
            int dupes = setting(args);
            if (dupes == -1)
            {
                player.ChatMessage(HelpMessage());
                return;
            }
            player.ChatMessage("Saved Map " + dupes.ToString() + " prefabs ignored");
        }
    }
}