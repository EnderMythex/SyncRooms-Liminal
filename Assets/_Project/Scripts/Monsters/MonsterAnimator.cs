using UnityEngine;

namespace SyncRooms.Monsters
{
    /// <summary>
    /// Procedural animation driven by Monster.Speed01. Parts are child transforms named in the Blender models
    /// (pivots at the joints). Each style animates a different creature.
    /// </summary>
    public class MonsterAnimator : MonoBehaviour
    {
        public enum Style { Hound, Moth, Smiler, Toon, Slime, Leviathan }
        public Style style;

        Monster _m;
        float _phase;
        Transform[] _parts;
        Quaternion[] _rest;
        Vector3 _baseScale;

        static readonly string[] HoundParts = { "LegFL", "LegFR", "LegBL", "LegBR", "LegFL_Low", "LegFR_Low", "LegBL_Low", "LegBR_Low", "Head", "Torso" };
        static readonly string[] MothParts = { "WingL", "WingR", "WingL2", "WingR2", "Body" };
        static readonly string[] ToonParts = { "ArmL", "ArmR", "LegL", "LegR", "Head", "Body" };

        void Start()
        {
            _m = GetComponentInParent<Monster>();
            _baseScale = transform.localScale;
            string[] names = style switch { Style.Hound => HoundParts, Style.Moth => MothParts, Style.Toon => ToonParts, _ => new string[0] };
            _parts = new Transform[names.Length];
            _rest = new Quaternion[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                _parts[i] = Find(transform, names[i]);
                if (_parts[i]) _rest[i] = _parts[i].localRotation;
            }
        }

        static Transform Find(Transform t, string n)
        {
            if (t.name == n) return t;
            foreach (Transform c in t) { var r = Find(c, n); if (r) return r; }
            return null;
        }

        void Rot(int i, Vector3 euler) { if (_parts[i]) _parts[i].localRotation = _rest[i] * Quaternion.Euler(euler); }

        void Update()
        {
            float spd = _m ? _m.Speed01 : 0.3f;
            bool chase = _m && _m.State == MonsterState.Chase;
            _phase += Time.deltaTime * Mathf.Lerp(2f, 13f, spd);
            float s = Mathf.Sin(_phase), c = Mathf.Cos(_phase);
            switch (style)
            {
                case Style.Hound:
                {
                    float a = 35f * Mathf.Clamp01(spd * 2f + 0.1f);
                    Rot(0, new Vector3(s * a, 0, 0)); Rot(3, new Vector3(s * a, 0, 0));
                    Rot(1, new Vector3(-s * a, 0, 0)); Rot(2, new Vector3(-s * a, 0, 0));
                    for (int i = 4; i < 8; i++) Rot(i, new Vector3(Mathf.Max(0, (i % 2 == 0 ? c : -c)) * a * 1.2f, 0, 0));
                    Rot(8, new Vector3(Mathf.Sin(_phase * 0.5f) * 8f, Mathf.Sin(Time.time * 0.7f) * 20f, chase ? Mathf.Sin(Time.time * 17f) * 6f : 0));
                    Rot(9, new Vector3(0, 0, s * 4f));
                    break;
                }
                case Style.Moth:
                {
                    float f = Mathf.Sin(Time.time * (chase ? 22f : 12f));
                    Rot(0, new Vector3(0, 0, f * 55f)); Rot(1, new Vector3(0, 0, -f * 55f));
                    Rot(2, new Vector3(0, 0, f * 45f)); Rot(3, new Vector3(0, 0, -f * 45f));
                    Rot(4, new Vector3(f * 5f, 0, 0));
                    break;
                }
                case Style.Smiler:
                    transform.localPosition = new Vector3(0, Mathf.Sin(Time.time * 1.3f) * 0.08f, 0);
                    transform.localScale = _baseScale * (chase ? 1f + Mathf.Sin(Time.time * 9f) * 0.04f : 1f);
                    break;
                case Style.Toon:
                {
                    float a = Mathf.Lerp(15f, 50f, spd);
                    Rot(0, new Vector3(s * a, 0, 0)); Rot(1, new Vector3(-s * a, 0, 0));
                    Rot(2, new Vector3(-s * a, 0, 0)); Rot(3, new Vector3(s * a, 0, 0));
                    Rot(4, new Vector3(0, 0, Mathf.Sin(_phase * 0.5f) * 10f));
                    transform.localPosition = new Vector3(0, Mathf.Abs(s) * 0.12f, 0);   // rubber-hose bounce
                    transform.localScale = new Vector3(_baseScale.x * (1f + Mathf.Abs(s) * 0.05f), _baseScale.y * (1f - Mathf.Abs(s) * 0.05f), _baseScale.z);
                    break;
                }
                case Style.Slime:
                {
                    float hop = Mathf.Abs(Mathf.Sin(_phase * 0.6f));
                    transform.localPosition = new Vector3(0, hop * (0.15f + spd * 0.5f), 0);
                    float squash = 1f + Mathf.Sin(_phase * 1.2f) * 0.12f;
                    transform.localScale = new Vector3(_baseScale.x * squash, _baseScale.y / squash, _baseScale.z * squash);
                    break;
                }
                case Style.Leviathan:
                {
                    // nested chain Seg0 > Seg1 > ... undulating like an eel
                    var t = transform;
                    int i = 0;
                    while (t.childCount > 0)
                    {
                        t = t.GetChild(0);
                        t.localRotation = Quaternion.Euler(0, Mathf.Sin(Time.time * 0.8f - i * 0.6f) * 12f, 0);
                        i++;
                    }
                    break;
                }
            }
        }
    }
}
