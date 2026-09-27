using SyncRooms.Gen;
using SyncRooms.Monsters;
using SyncRooms.Player;
using SyncRooms.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace SyncRooms.EditorTools
{
    /// <summary>Scenes whose geometry is streamed at runtime (endless levels): 4, 37, 1, 2.</summary>
    public static class StreamedLevels
    {
        public static T World<T>(Transform root, float chunk, int radius, int seed, Vector3 origin = default) where T : ChunkBuilder
        {
            var go = new GameObject("World (streamed)");
            go.transform.SetParent(root, false);
            var b = go.AddComponent<T>();
            var w = go.AddComponent<ChunkWorld>();
            w.builder = b; w.chunkSize = chunk; w.radius = radius; w.seed = seed; w.origin = origin;
            return b;
        }

        public static MonsterSpawner Spawner(Transform root, int max, float firstDelay, float interval, params string[] prefabs)
        {
            var go = new GameObject("Monster Spawner");
            go.transform.SetParent(root, false);
            var s = go.AddComponent<MonsterSpawner>();
            s.prefabs = prefabs; s.maxAlive = max; s.firstDelay = firstDelay; s.interval = interval;
            return s;
        }

        static Transform Spawn(Transform root, Vector3 p, float yaw)
        {
            var t = new GameObject("Spawn").transform;
            t.SetParent(root, false);
            t.SetPositionAndRotation(p, Quaternion.Euler(0, yaw, 0));
            return t;
        }

        [MenuItem("SyncRooms/Levels/Build Level 4 (endless)")]
        public static void BuildLevel4()
        {
            var scene = LevelKit.NewScene("Level4");
            var root = LevelKit.Group("Level4").transform;
            World<OfficeChunks>(root, 24f, 2, 404);
            FollowProbe.Create(root, new Vector3(30, 6, 30));
            LevelKit.GlobalVolume(root, "Level4", new LevelKit.Look
            {
                expMin = 7.3f, expMax = 10.5f, expComp = -0.6f, fogMFP = 26f, fogMaxHeight = 3f, fogAlbedo = new Color(0.95f, 0.95f, 0.86f),
                bloom = 0.3f, vignette = 0.28f, grain = 0.22f, saturation = -12f, contrast = 6f, temperature = 6f, tint = -4f,
                skyType = 3, gradientTop = new Color(0.02f, 0.02f, 0.02f), gradientMid = new Color(0.02f, 0.02f, 0.02f), gradientBottom = new Color(0.01f, 0.01f, 0.01f),
                shadowDistance = 30f, chroma = 0.08f
            });
            var sp = Spawn(root, new Vector3(12f, 0.05f, 12f), 0);
            LevelKit.Player(root, sp.position, 0);
            var ls = LevelKit.Settings(root, sp, 1.0f, 1.5f, new[] { "Ambience/amb_office_hvac", "Ambience/amb_fluorescent_hum" }, 0.28f);
            ls.arrival = ArrivalKind.WakeUp;
            ls.GetComponent<LevelAmbience>().oneShots = new[]
            {
                new LevelAmbience.RandomOneShot { clip = "Ambience/amb_distant_thud", minDelay = 25, maxDelay = 70, volume = 0.5f, minDist = 12, maxDist = 25 },
                new LevelAmbience.RandomOneShot { clip = "Monsters/hound_growl", minDelay = 60, maxDelay = 140, volume = 0.3f, minDist = 20, maxDist = 30 },
            };
            Spawner(root, 3, 25f, 35f, "Hound");
            LevelKit.SaveScene(scene, "Level4");
        }

        [MenuItem("SyncRooms/Levels/Build Level 37 (endless)")]
        public static void BuildLevel37()
        {
            var scene = LevelKit.NewScene("Level37");
            var root = LevelKit.Group("Level37").transform;
            World<PoolChunks>(root, 14.6f, 2, 37);
            FollowProbe.Create(root, new Vector3(20, 8, 20));
            LevelKit.Sun(root, new Vector3(68, -30, 0), 100000f, 5800f);
            LevelKit.GlobalVolume(root, "Level37", new LevelKit.Look
            {
                expMin = 9.5f, expMax = 13.5f, expComp = 0.2f, fogMFP = 36f, fogMaxHeight = 6f, fogAlbedo = new Color(0.92f, 0.98f, 1f),
                bloom = 0.35f, vignette = 0.15f, grain = 0.08f, saturation = -4f, contrast = 6f, temperature = -4f,
                skyType = (int)SkyType.PhysicallyBased, shadowDistance = 60f, chroma = 0.02f
            });
            var sp = Spawn(root, new Vector3(7.3f, -0.4f, 4.3f), 0);
            LevelKit.Player(root, sp.position, 0);
            var ls = LevelKit.Settings(root, sp, -1.2f, 0f, new[] { "Ambience/amb_pool" }, 0.3f);
            ls.arrival = ArrivalKind.Emerge;
            ls.GetComponent<LevelAmbience>().oneShots = new[]
            {
                new LevelAmbience.RandomOneShot { clip = "Ambience/amb_drip", minDelay = 6, maxDelay = 18, volume = 0.45f, minDist = 3, maxDist = 14 },
            };
            var rz = new GameObject("Reverb").AddComponent<AudioReverbZone>();
            rz.transform.SetParent(root, false);
            rz.reverbPreset = AudioReverbPreset.Bathroom;
            rz.minDistance = 5000f; rz.maxDistance = 6000f;
            LevelKit.SaveScene(scene, "Level37");
        }

        [MenuItem("SyncRooms/Levels/Build Level 1 (Parking Zone)")]
        public static void BuildLevel1()
        {
            var scene = LevelKit.NewScene("Level1");
            var root = LevelKit.Group("Level1").transform;
            World<ParkingChunks>(root, 24f, 2, 101);
            FollowProbe.Create(root, new Vector3(30, 6, 30));
            LevelKit.GlobalVolume(root, "Level1", new LevelKit.Look
            {
                expMin = 7.5f, expMax = 10.5f, expComp = -0.7f, fogMFP = 20f, fogMaxHeight = 3.2f, fogAlbedo = new Color(0.85f, 0.9f, 0.9f),
                bloom = 0.3f, vignette = 0.3f, grain = 0.25f, saturation = -20f, contrast = 10f, temperature = -8f, tint = 6f,
                skyType = 3, gradientTop = new Color(0.01f, 0.01f, 0.01f), gradientMid = new Color(0.01f, 0.01f, 0.01f), gradientBottom = new Color(0.01f, 0.01f, 0.01f),
                shadowDistance = 30f
            });
            var sp = Spawn(root, new Vector3(12f, 0.05f, 12.6f), 0);
            LevelKit.Player(root, sp.position, 0);
            var ls = LevelKit.Settings(root, sp, 1.0f, 1.5f, new[] { "Ambience/amb_garage", "Ambience/amb_fluorescent_hum" }, 0.3f);
            ls.arrival = ArrivalKind.Walk;
            ls.GetComponent<LevelAmbience>().oneShots = new[]
            {
                new LevelAmbience.RandomOneShot { clip = "Ambience/amb_drip", minDelay = 8, maxDelay = 20, volume = 0.4f, minDist = 4, maxDist = 14 },
                new LevelAmbience.RandomOneShot { clip = "Monsters/slime_squelch", minDelay = 30, maxDelay = 70, volume = 0.35f, minDist = 12, maxDist = 25 },
            };
            Spawner(root, 5, 12f, 18f, "Slime", "Slime", "SlimeBig");
            LevelKit.SaveScene(scene, "Level1");
        }

        [MenuItem("SyncRooms/Levels/Build Level 2 (Pipe Dreams)")]
        public static void BuildLevel2()
        {
            var scene = LevelKit.NewScene("Level2");
            var root = LevelKit.Group("Level2").transform;
            World<PipeChunks>(root, 24f, 3, 202);
            FollowProbe.Create(root, new Vector3(6, 4, 30));
            LevelKit.GlobalVolume(root, "Level2", new LevelKit.Look
            {
                expMin = 7f, expMax = 10f, expComp = -0.5f, fogMFP = 18f, fogMaxHeight = 3.2f, fogAlbedo = new Color(1f, 0.85f, 0.7f),
                bloom = 0.35f, vignette = 0.35f, grain = 0.25f, saturation = -8f, contrast = 12f, temperature = 18f,
                skyType = 3, gradientTop = new Color(0.01f, 0.01f, 0.01f), gradientMid = new Color(0.01f, 0.01f, 0.01f), gradientBottom = new Color(0.01f, 0.01f, 0.01f),
                shadowDistance = 25f, chroma = 0.1f
            });
            var sp = Spawn(root, new Vector3(12f, 0.05f, 3f), 0);
            LevelKit.Player(root, sp.position, 0);
            var ls = LevelKit.Settings(root, sp, 1.5f, 2.5f, new[] { "Ambience/amb_boiler" }, 0.25f);
            ls.arrival = ArrivalKind.Walk;
            ls.GetComponent<LevelAmbience>().oneShots = new[]
            {
                new LevelAmbience.RandomOneShot { clip = "Ambience/amb_steam", minDelay = 10, maxDelay = 25, volume = 0.5f, minDist = 4, maxDist = 12 },
                new LevelAmbience.RandomOneShot { clip = "Ambience/amb_distant_thud", minDelay = 20, maxDelay = 50, volume = 0.5f, minDist = 12, maxDist = 25 },
            };
            var s = Spawner(root, 3, 15f, 25f, "Smiler", "Smiler", "Hound");
            s.minDistance = 16f; s.maxDistance = 30f;
            LevelKit.SaveScene(scene, "Level2");
        }
    }
}
