using System.Collections.Generic;
using System.IO;
using SyncRooms.Inventory;
using SyncRooms.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace SyncRooms.EditorTools
{
    /// <summary>Creates item definitions, pickup prefabs and rendered icons, plus the Resources/ItemDatabase.</summary>
    public static class ItemLibrary
    {
        const string DefDir = "Assets/_Project/Data/Items";
        const string PrefabDir = "Assets/_Project/Prefabs/Items";
        const string IconDir = "Assets/_Project/Art/Icons";

        public class Spec
        {
            public string id, name, desc, model, note;
            public ItemUse use;
            public float amount;
            public int stack = 1;
            public bool consume = true;
            public Vector3 iconEuler = new(20, -35, 0);
            public float modelScale = 1f;
        }

        public static readonly List<Spec> Specs = new()
        {
            new Spec { id = "almond_water", name = "Eau d'amande", model = "Item_AlmondWater", use = ItemUse.Drink, amount = 30, stack = 5,
                desc = "Almond Water. Légèrement sucrée, elle apaise l'esprit et restaure la santé mentale. La ressource la plus précieuse des Backrooms." },
            new Spec { id = "flashlight", name = "Lampe torche", model = "vintage_flashlight", use = ItemUse.Flashlight, consume = false,
                desc = "Une vieille lampe en métal. [F] pour l'allumer. Consomme de la batterie.", iconEuler = new Vector3(15, -60, -20) },
            new Spec { id = "battery", name = "Pile", model = "Item_Battery", use = ItemUse.Battery, amount = 50, stack = 6,
                desc = "Pile type D. Recharge la lampe torche de 50 %." },
            new Spec { id = "keycard_l4", name = "Badge d'accès", model = "Item_Keycard", use = ItemUse.Key, consume = false,
                desc = "« SERVICES GÉNÉRAUX — ACCÈS ESCALIER B ». La photo a été effacée.", iconEuler = new Vector3(60, 0, 15) },
            new Spec { id = "hotel_key", name = "Clé de la chambre 237", model = "Item_HotelKey", use = ItemUse.Key, consume = false,
                desc = "Une clé en laiton, étonnamment froide. L'étiquette porte le numéro 237.", iconEuler = new Vector3(70, 0, 30) },
            new Spec { id = "medkit", name = "Trousse de secours", model = "medical_box", use = ItemUse.Heal, amount = 50,
                desc = "Bandages, calmants, une seringue sans étiquette. Restaure beaucoup de santé mentale.", modelScale = 0.5f },
            new Spec { id = "ration", name = "Ration de survie", model = "long_life_food", use = ItemUse.Eat, amount = 15, stack = 3,
                desc = "Conserve longue durée. Le goût importe peu ici.", modelScale = 0.6f },
            new Spec { id = "pocket_watch", name = "Montre à gousset", model = "pocket_watch", use = ItemUse.None, consume = false,
                desc = "Elle avance, puis recule. Les aiguilles indiquent toujours 3 h 33." },
            new Spec { id = "lighter", name = "Briquet", model = "vintage_lighter", use = ItemUse.None, consume = false,
                desc = "Il ne s'allume plus. Gravé : « M.E.G. — Base Alpha »." },
            new Spec { id = "cassette", name = "Cassette audio", model = "portable_cassette_player", use = ItemUse.Read, consume = false,
                desc = "Un baladeur avec une cassette encore dedans.",
                note = "[Transcription — la bande grésille]\n\n« ...jour 41. Le bourdonnement ne s'arrête jamais. Je l'entends même quand je bouche mes oreilles.\n\nSi vous trouvez ça : ne courez pas dans les couloirs du bureau. Marchez. Les néons clignotent quand quelque chose approche.\n\nL'escalier B mène à l'hôtel. Il faut un badge. J'ai laissé le mien près des distributeurs... ou peut-être pas. Je ne sais plus.\n\nBuvez l'eau d'amande. Toujours. »\n\n[fin de la bande]" },
            new Spec { id = "note_l4_1", name = "Note froissée", model = "Item_Note", use = ItemUse.Read, consume = false, iconEuler = new Vector3(70, 0, 10),
                desc = "Une page arrachée d'un bloc-notes.",
                note = "Si tu lis ça, tu as « noclippé » comme moi.\n\nCe n'est PAS un bureau normal. Les fenêtres sont peintes en noir pour une bonne raison. NE REGARDE PAS À TRAVERS celles qui ne le sont pas.\n\nLes fontaines à eau donnent de l'eau d'amande. Remplis tes bouteilles.\n\nL'escalier de service est verrouillé. Le badge doit être quelque part dans l'open space.\n\n— R." },
            new Spec { id = "note_l5_1", name = "Carte de l'hôtel", model = "Item_Note", use = ItemUse.Read, consume = false, iconEuler = new Vector3(70, 0, -10),
                desc = "Papier à en-tête de l'hôtel.",
                note = "HÔTEL — RÈGLEMENT INTÉRIEUR\n\n1. Les clients sont priés de ne pas s'attarder dans la salle Beverly.\n2. La partie de mahjong n'est pas terminée. N'y touchez pas.\n3. Si vous entendez des murmures, continuez à marcher.\n4. La chambre 237 donne sur « l'extérieur ». La clé est gardée derrière la réception.\n5. La chaufferie est interdite au public.\n\nLa Direction vous souhaite un agréable séjour. Éternel." },
            new Spec { id = "note_94_1", name = "Tract du village", model = "Item_Note", use = ItemUse.Read, consume = false, iconEuler = new Vector3(70, 0, 5),
                desc = "Un tract jauni aux couleurs vives.",
                note = "BIENVENUE À MOTION !\n\nProfitez du soleil, de la musique et de nos charmantes maisons !\n\nQuand les sirènes jouent leur petit air, tout va bien.\nQuand la musique S'ARRÊTE, rentrez chez vous.\n\nLa fontaine de la place déborde d'eau d'amande. Certains disent qu'en plongeant dedans, on se réveille ailleurs — dans une eau calme et chaude, sous une lumière très blanche.\n\nNe regardez pas trop longtemps le château." },
            new Spec { id = "note_37_1", name = "Page mouillée", model = "Item_Note", use = ItemUse.Read, consume = false, iconEuler = new Vector3(70, 0, 0),
                desc = "L'encre a coulé, mais c'est lisible.",
                note = "Ici, rien ne me veut du mal.\n\nL'eau est chaude, elle sent le sel. Les sons sont étouffés, comme sous une couverture. Je n'ai croisé personne depuis des jours. Je crois que je n'en ai pas envie.\n\nLes tunnels sombres au fond mènent ailleurs. Je ne suis pas pressé.\n\nC'est la première fois que je dors vraiment." },
        };

        [MenuItem("SyncRooms/4. Build Items (defs, prefabs, icons)")]
        public static void BuildAll()
        {
            Directory.CreateDirectory(DefDir);
            Directory.CreateDirectory(PrefabDir);
            Directory.CreateDirectory(IconDir);
            var defs = new List<ItemDefinition>();
            foreach (var s in Specs)
            {
                var def = AssetDatabase.LoadAssetAtPath<ItemDefinition>($"{DefDir}/{s.id}.asset");
                if (!def)
                {
                    def = ScriptableObject.CreateInstance<ItemDefinition>();
                    AssetDatabase.CreateAsset(def, $"{DefDir}/{s.id}.asset");
                }
                def.id = s.id;
                def.displayName = s.name;
                def.description = s.desc;
                def.use = s.use;
                def.amount = s.amount;
                def.maxStack = s.stack;
                def.consumeOnUse = s.consume;
                def.noteText = s.note;
                def.worldPrefab = MakePickup(s, def);
                EditorUtility.SetDirty(def);
                defs.Add(def);
            }
            AssetDatabase.SaveAssets();

            RenderIcons(defs);
            // NewScene(Single) unloads unreferenced assets: reload the definitions from disk.
            defs = Specs.ConvertAll(sp => Get(sp.id));

            Directory.CreateDirectory("Assets/_Project/Resources");
            var db = AssetDatabase.LoadAssetAtPath<ItemDatabase>("Assets/_Project/Resources/ItemDatabase.asset");
            if (!db)
            {
                db = ScriptableObject.CreateInstance<ItemDatabase>();
                AssetDatabase.CreateAsset(db, "Assets/_Project/Resources/ItemDatabase.asset");
            }
            db.items = defs;
            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
            Debug.Log("[SyncRooms] Items built: " + defs.Count);
        }

        public static ItemDefinition Get(string id) => AssetDatabase.LoadAssetAtPath<ItemDefinition>($"{DefDir}/{id}.asset");

        static GameObject MakePickup(Spec s, ItemDefinition def)
        {
            var model = ModelLibrary.Get(s.model);
            if (!model) return null;
            var root = new GameObject(s.id);
            var m = (GameObject)PrefabUtility.InstantiatePrefab(model);
            m.transform.SetParent(root.transform, false);
            m.transform.localScale = Vector3.one * s.modelScale;
            foreach (var c in m.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            var b = Bounds(m);
            var bc = root.AddComponent<BoxCollider>();
            bc.center = b.center;
            bc.size = Vector3.Max(b.size, Vector3.one * 0.04f);
            var rb = root.AddComponent<Rigidbody>();
            rb.mass = Mathf.Clamp(b.size.magnitude * 2f, 0.2f, 4f);
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            var wi = root.AddComponent<WorldItem>();
            wi.item = def;
            var path = $"{PrefabDir}/{s.id}.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        static Bounds Bounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return new Bounds(Vector3.zero, Vector3.one * 0.1f);
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        // --------------------------------------------------------------- icons
        static void RenderIcons(List<ItemDefinition> defs)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            try
            {
                var camGo = new GameObject("IconCam");
                SceneManager_Move(camGo, scene);
                var cam = camGo.AddComponent<Camera>();
                cam.fieldOfView = 22f;
                cam.nearClipPlane = 0.01f;
                var hd = camGo.AddComponent<HDAdditionalCameraData>();
                hd.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
                hd.volumeLayerMask = 1 << 31;
                hd.antialiasing = HDAdditionalCameraData.AntialiasingMode.SubpixelMorphologicalAntiAliasing;

                var volGo = new GameObject("IconVolume") { layer = 31 };
                SceneManager_Move(volGo, scene);
                var vol = volGo.AddComponent<Volume>();
                vol.isGlobal = true;
                var profile = ScriptableObject.CreateInstance<VolumeProfile>();
                var exp = profile.Add<Exposure>(true);
                exp.mode.Override(ExposureMode.Fixed);
                exp.fixedExposure.Override(11.5f);
                var tm = profile.Add<Tonemapping>(true);
                tm.mode.Override(TonemappingMode.Neutral);
                var sky = profile.Add<VisualEnvironment>(true);
                sky.skyType.Override((int)SkyType.Gradient);
                var amb = profile.Add<GradientSky>(true);
                amb.top.Override(new Color(0.8f, 0.85f, 1f));
                amb.middle.Override(new Color(0.6f, 0.6f, 0.6f));
                amb.bottom.Override(new Color(0.25f, 0.22f, 0.2f));
                amb.multiplier.Override(1.5f);
                var fog = profile.Add<Fog>(true);
                fog.enabled.Override(false);
                vol.profile = profile;

                Light MakeLight(string n, Vector3 euler, float lux, Color c)
                {
                    var go = new GameObject(n);
                    SceneManager_Move(go, scene);
                    go.transform.eulerAngles = euler;
                    var l = go.AddComponent<Light>();
                    l.type = LightType.Directional;
                    l.color = c;
                    l.intensity = lux;
                    l.shadows = LightShadows.Soft;
                    return l;
                }
                MakeLight("Key", new Vector3(40, -30, 0), 4000f, new Color(1f, 0.96f, 0.9f));
                MakeLight("Rim", new Vector3(20, 150, 0), 2500f, new Color(0.8f, 0.9f, 1f));

                foreach (var s0 in Specs)
                {
                    var def = Get(s0.id);
                    var spec = s0;
                    if (def.worldPrefab == null) continue;
                    var inst = (GameObject)PrefabUtility.InstantiatePrefab(def.worldPrefab, scene);
                    inst.transform.position = Vector3.zero;
                    inst.transform.rotation = Quaternion.Euler(spec.iconEuler);
                    var b = Bounds(inst);
                    float radius = b.extents.magnitude;
                    float dist = radius / Mathf.Sin(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.05f;
                    camGo.transform.position = b.center + new Vector3(0, 0, -dist);
                    camGo.transform.LookAt(b.center);
                    cam.nearClipPlane = Mathf.Max(0.005f, dist - radius * 2f);
                    cam.farClipPlane = dist + radius * 2f;

                    hd.backgroundColorHDR = Color.black;
                    var black = EditorCapture.Render(cam, 256, 256);
                    hd.backgroundColorHDR = Color.white;
                    var white = EditorCapture.Render(cam, 256, 256);
                    var outTex = Composite(black, white);
                    string path = $"{IconDir}/{def.id}.png";
                    EditorCapture.SavePng(outTex, path, true);
                    Object.DestroyImmediate(black);
                    Object.DestroyImmediate(white);
                    Object.DestroyImmediate(outTex);
                    Object.DestroyImmediate(inst);
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                    def.icon = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    EditorUtility.SetDirty(def);
                }
            }
            finally { }
        }

        static void SceneManager_Move(GameObject go, UnityEngine.SceneManagement.Scene s) =>
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, s);

        /// <summary>Recover alpha from renders over black and white backgrounds.</summary>
        static Texture2D Composite(Texture2D black, Texture2D white)
        {
            var b = black.GetPixels();
            var w = white.GetPixels();
            // background response measured in the corner
            var bw = w[0];
            var bb = b[0];
            float bgDelta = Mathf.Max(0.001f, ((bw.r - bb.r) + (bw.g - bb.g) + (bw.b - bb.b)) / 3f);
            var o = new Color[b.Length];
            for (int i = 0; i < b.Length; i++)
            {
                float d = ((w[i].r - b[i].r) + (w[i].g - b[i].g) + (w[i].b - b[i].b)) / 3f / bgDelta;
                float a = Mathf.Clamp01(1f - d);
                var c = a > 0.001f ? new Color(b[i].r / a, b[i].g / a, b[i].b / a, a) : new Color(0, 0, 0, 0);
                o[i] = c;
            }
            var t = new Texture2D(black.width, black.height, TextureFormat.RGBAFloat, false, true);
            t.SetPixels(o);
            t.Apply();
            return t;
        }
    }
}
