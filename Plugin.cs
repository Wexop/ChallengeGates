using System;
using BepInEx;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx.Configuration;
using ChallengeGates.Utils;
using LethalConfig;
using LethalConfig.ConfigItems;
using LethalConfig.ConfigItems.Options;
using LethalLib.Extras;
using UnityEngine;
using LethalLib.Modules;
using Unity.Netcode;
using NetworkPrefabs = LethalLib.Modules.NetworkPrefabs;

namespace ChallengeGates
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class ChallengeGatesPlugin : BaseUnityPlugin
    {

        const string GUID = "wexop.challenge_gates";
        const string NAME = "ChallengeGates";
        const string VERSION = "1.0.0";

        public static ChallengeGatesPlugin instance;
        
        public ConfigEntry<int> maxSpawn;
        public ConfigEntry<int> minSpawn;
        public ConfigEntry<int> baseTrophyValue;
        public ConfigEntry<int> trophyDecreaseAmount;
        public ConfigEntry<int> trophyDecreaseDelay;
        
        public ConfigEntry<bool> debug;


        public GameObject trophyGameObject;
        public GameObject gateGameObject;

        private float timer;
        
        private void Update()
        {
            if(!debug.Value) return;
            
            timer -= Time.deltaTime;
            if(timer > 0) return;
            
            var drop = IngamePlayerSettings.Instance.playerInput.actions.FindAction("Discard", false).ReadValue<float>();
            
            if (drop > 0)
            {
                if (gateGameObject != null)
                {
                    var o = Instantiate(gateGameObject,
                        GameNetworkManager.Instance.localPlayerController.transform.position +
                        GameNetworkManager.Instance.localPlayerController.transform.forward * 15, Quaternion.identity);

                    var n = o.GetComponent<NetworkObject>();
                    if(n.IsOwner) n.Spawn();
                    timer = 5;
                }
            }
        }

        void Awake()
        {
            instance = this;
            
            Logger.LogInfo($"ChallengeGates starting....");

            string assetDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "challengegates");
            AssetBundle bundle = AssetBundle.LoadFromFile(assetDir);
            
            Logger.LogInfo($"ChallengeGates bundle found !");
            
            NetcodePatcher();
            LoadConfigs();
            RegisterScrap(bundle);
            RegisterHazard(bundle);
            
            
            Logger.LogInfo($"ChallengeGates is ready!");
        }

        string RarityString(int rarity)
        {
            return
                $"Modded:{rarity},ExperimentationLevel:{rarity},AssuranceLevel:{rarity},VowLevel:{rarity},OffenseLevel:{rarity},MarchLevel:{rarity},RendLevel:{rarity},DineLevel:{rarity},TitanLevel:{rarity},Adamance:{rarity},Embrion:{rarity},Artifice:{rarity}";

        }

        void LoadConfigs()
        {
            
            //GENERAL
            
            minSpawn = Config.Bind(
                "General", 
                "MinSpawn", 
                0,
                "Min challenge gate possible for one game. You need to restart the game.");
            CreateIntConfig(minSpawn, restart:true);
            
            maxSpawn = Config.Bind(
                "General", 
                "MaxSpawn", 
                7,
                "Max challenge gate possible for one game. You need to restart the game.");
            CreateIntConfig(maxSpawn, restart:true);
            
            // TROPHY
            baseTrophyValue = Config.Bind(
                "Trophy", 
                "BaseTrophyValue", 
                300,
                "Base trophy value. Note that value decrease will you do the challenge and stop when you grab the trophy. You don't need to restart the game :)");
            CreateIntConfig(baseTrophyValue, 0, 750);
            
            trophyDecreaseAmount = Config.Bind(
                "Trophy", 
                "TrophyDecreaseAmount", 
                10,
                "Trophy value decrease with time. Amount that trophy lose each time. You don't need to restart the game :)");
            CreateIntConfig(trophyDecreaseAmount);
            
            trophyDecreaseDelay = Config.Bind(
                "Trophy", 
                "TrophyDecreaseDelay", 
                2,
                "Trophy value decrease with time. Delay in seconds. You don't need to restart the game :)");
            CreateIntConfig(trophyDecreaseDelay, 0, 60);

            
            //DEV
            
            debug = Config.Bind(
                "DEV", 
                "Debug", 
                false,
                "Enable debug");
            CreateBoolConfig(debug);
        }
        
        void RegisterScrap(AssetBundle bundle)
        {
            Item trophy = bundle.LoadAsset<Item>("Assets/LethalCompany/Mods/ChallengeGates/Trophy.asset");
            Logger.LogInfo($"{trophy.name} FOUND");
            Logger.LogInfo($"{trophy.spawnPrefab} prefab");
            NetworkPrefabs.RegisterNetworkPrefab(trophy.spawnPrefab);
            Utilities.FixMixerGroups(trophy.spawnPrefab);
            RegisterUtil.RegisterScrapWithConfig("", trophy );
            trophyGameObject = trophy.spawnPrefab;
        }
        
        void RegisterHazard(AssetBundle bundle)
        {
            SpawnableMapObjectDef gate =
                bundle.LoadAsset<SpawnableMapObjectDef>("Assets/LethalCompany/Mods/ChallengeGates/ChallengeGate.asset");
            Logger.LogInfo($"{gate.spawnableMapObject.prefabToSpawn.name} FOUND");
            
            AnimationCurve curve = new AnimationCurve(new Keyframe(0, instance.minSpawn.Value, 0.267f, 0.267f, 0, 0),
                new Keyframe(1, instance.maxSpawn.Value, 61, 61, 0.015f * instance.maxSpawn.Value, 0));

            gate.spawnableMapObject.numberToSpawn = curve;
            
            NetworkPrefabs.RegisterNetworkPrefab(gate.spawnableMapObject.prefabToSpawn);
            Utilities.FixMixerGroups(gate.spawnableMapObject.prefabToSpawn);
            
            MapObjects.RegisterMapObject(gate, Levels.LevelTypes.All, _ => curve);
            
            gateGameObject = gate.spawnableMapObject.prefabToSpawn;
            
        }
        
        /// <summary>
        ///     Slightly modified version of: https://github.com/EvaisaDev/UnityNetcodePatcher?tab=readme-ov-file#preparing-mods-for-patching
        /// </summary>
        private static void NetcodePatcher()
        {
            Type[] types;
            try
            {
                types = Assembly.GetExecutingAssembly().GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                // This goofy try catch is needed here to be able to use soft dependencies in the future, though none are present at the moment.
                types = e.Types.Where(type => type != null).ToArray();
            }

            foreach (Type type in types)
            {
                foreach (MethodInfo method in type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                {
                    if (method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false).Length > 0)
                    {
                        // Do weird magic...
                        _ = method.Invoke(null, null);
                    }
                }
            }
        }
        private void CreateFloatConfig(ConfigEntry<float> configEntry, float min = 0f, float max = 100f)
        {
            var exampleSlider = new FloatSliderConfigItem(configEntry, new FloatSliderOptions
            {
                Min = min,
                Max = max,
                RequiresRestart = false
            });
            LethalConfigManager.AddConfigItem(exampleSlider);
        }
        
        private void CreateIntConfig(ConfigEntry<int> configEntry, int min = 0, int max = 100, bool restart=false)
        {
            var exampleSlider = new IntSliderConfigItem(configEntry, new IntSliderOptions()
            {
                Min = min,
                Max = max,
                RequiresRestart = restart
            });
            LethalConfigManager.AddConfigItem(exampleSlider);
        }
        
        private void CreateStringConfig(ConfigEntry<string> configEntry, bool requireRestart = false)
        {
            var exampleSlider = new TextInputFieldConfigItem(configEntry, new TextInputFieldOptions()
            {
                RequiresRestart = requireRestart
            });
            LethalConfigManager.AddConfigItem(exampleSlider);
        }
        
        public bool StringContain(string name, string verifiedName)
        {
            var name1 = name.ToLower();
            while (name1.Contains(" ")) name1 = name1.Replace(" ", "");

            var name2 = verifiedName.ToLower();
            while (name2.Contains(" ")) name2 = name2.Replace(" ", "");

            return name1.Contains(name2);
        }
        
        private void CreateBoolConfig(ConfigEntry<bool> configEntry)
        {
            var exampleSlider = new BoolCheckBoxConfigItem(configEntry, new BoolCheckBoxOptions
            {
                RequiresRestart = false
            });
            LethalConfigManager.AddConfigItem(exampleSlider);
        }
        
    }
}