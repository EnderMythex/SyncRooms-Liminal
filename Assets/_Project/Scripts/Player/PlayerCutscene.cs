using System.Collections;
using SyncRooms.Core;
using UnityEngine;

namespace SyncRooms.Player
{
    public enum ExitKind { None, Walk, Stairs, Window, Dive, Ladder, Elevator }
    public enum ArrivalKind { None, WakeUp, Elevator, FallIn, Emerge, Stairs, Walk, Ladder }

    /// <summary>First-person camera choreography for level exits and arrivals.</summary>
    public static class PlayerCutscene
    {
        static FirstPersonController P => Object.FindAnyObjectByType<FirstPersonController>();

        static float Ease(float t) => t * t * (3f - 2f * t);

        /// <summary>Moves the player root from a to b while the camera pivot goes to local eye offset + pitch/roll.</summary>
        static IEnumerator Move(FirstPersonController p, Vector3 a, Vector3 b, float yawA, float yawB, float pitchA, float pitchB,
            float eyeA, float eyeB, float dur, float bob = 0f, float rollA = 0f, float rollB = 0f)
        {
            for (float t = 0; t < dur; t += Time.deltaTime)
            {
                float k = Ease(Mathf.Clamp01(t / dur));
                p.transform.position = Vector3.Lerp(a, b, k);
                float yaw = Mathf.LerpAngle(yawA, yawB, k);
                p.transform.rotation = Quaternion.Euler(0, yaw, 0);
                float eye = Mathf.Lerp(eyeA, eyeB, k) + Mathf.Sin(t * 11f) * bob;
                p.cameraPivot.localPosition = new Vector3(0, eye, 0);
                p.cameraPivot.localRotation = Quaternion.Euler(Mathf.Lerp(pitchA, pitchB, k), 0, Mathf.Lerp(rollA, rollB, k));
                yield return null;
            }
            p.transform.position = b;
            p.transform.rotation = Quaternion.Euler(0, yawB, 0);
        }

        static void Begin(FirstPersonController p)
        {
            GameManager.Instance.InCutscene = true;
            p.Frozen = true;
            p.Controller.enabled = false;
        }

        static void End(FirstPersonController p)
        {
            p.cameraPivot.localPosition = new Vector3(0, p.EyeHeight, 0);
            p.cameraPivot.localRotation = Quaternion.identity;
            p.SetView(p.transform.eulerAngles.y, 0);
            p.Controller.enabled = true;
            p.Frozen = false;
            GameManager.Instance.InCutscene = false;
        }

        // ------------------------------------------------------------------ exits
        public static IEnumerator Exit(ExitKind kind, Transform anchor)
        {
            var p = P;
            if (p == null || kind == ExitKind.None) yield break;
            Begin(p);
            var gm = GameManager.Instance;
            Vector3 start = p.transform.position;
            float yaw0 = p.Yaw, eye = p.EyeHeight;
            // face the anchor
            Vector3 to = anchor.position - start; to.y = 0;
            float yawT = to.sqrMagnitude > 0.01f ? Quaternion.LookRotation(to).eulerAngles.y : yaw0;
            Vector3 fwd = Quaternion.Euler(0, yawT, 0) * Vector3.forward;
            Vector3 at = new Vector3(anchor.position.x, start.y, anchor.position.z);
            switch (kind)
            {
                case ExitKind.Walk:
                    yield return Move(p, start, at - fwd * 0.4f, yaw0, yawT, 0, 0, eye, eye, 1.0f, 0.02f);
                    gm.StartCoroutine(gm.UI.Fade(1f, 0.9f));
                    yield return Move(p, at - fwd * 0.4f, at + fwd * 1.6f, yawT, yawT, 0, 5, eye, eye, 1.2f, 0.03f);
                    break;
                case ExitKind.Stairs:
                    yield return Move(p, start, at, yaw0, yawT, 0, 20, eye, eye, 1.0f, 0.02f);
                    gm.StartCoroutine(gm.UI.Fade(1f, 1.8f));
                    yield return Move(p, at, at + fwd * 3.2f + Vector3.down * 2.0f, yawT, yawT, 20, 25, eye, eye, 2.2f, 0.05f);
                    break;
                case ExitKind.Window:
                    yield return Move(p, start, at - fwd * 0.5f, yaw0, yawT, 0, 10, eye, eye, 1.0f, 0.02f);
                    Sfx.Play2D("SFX/sfx_door_open", 0.6f);
                    yield return Move(p, at - fwd * 0.5f, at + Vector3.up * 0.8f, yawT, yawT, 10, 30, eye, eye - 0.7f, 1.1f);
                    gm.StartCoroutine(gm.UI.Fade(1f, 0.9f));
                    yield return Move(p, at + Vector3.up * 0.8f, at + fwd * 1.2f + Vector3.down * 1.0f, yawT, yawT, 30, 70, eye - 0.7f, eye - 0.9f, 0.8f, 0, 0, 25);
                    break;
                case ExitKind.Dive:
                    yield return Move(p, start, at - fwd * 0.3f, yaw0, yawT, 0, 35, eye, eye, 1.0f, 0.02f);
                    yield return Move(p, at - fwd * 0.3f, at + fwd * 0.6f + Vector3.up * 0.4f, yawT, yawT, 35, 60, eye, eye, 0.35f);
                    Sfx.Play2D("SFX/sfx_splash_dive", 1f);
                    gm.StartCoroutine(gm.UI.Fade(1f, 1.2f));
                    yield return Move(p, at + fwd * 0.6f + Vector3.up * 0.4f, at + fwd * 1.4f + Vector3.down * 2.2f, yawT, yawT, 60, 85, eye, eye, 1.1f);
                    Sfx.Play2D("SFX/sfx_underwater", 0.8f, 0);
                    break;
                case ExitKind.Ladder:
                    yield return Move(p, start, at - fwd * 0.45f, yaw0, yawT, 0, 15, eye, eye, 1.0f, 0.02f);
                    gm.StartCoroutine(gm.UI.Fade(1f, 2.2f));
                    for (int i = 0; i < 4; i++)
                    {
                        Sfx.Play2D("Footsteps/fs_concrete", 0.5f);
                        var a = at - fwd * 0.45f + Vector3.down * (i * 0.6f);
                        yield return Move(p, a, a + Vector3.down * 0.6f, yawT, yawT, 15 + i * 5, 20 + i * 5, eye, eye, 0.55f);
                    }
                    break;
                case ExitKind.Elevator:
                    yield return Move(p, start, at + fwd * 0.6f, yaw0, yawT, 0, 0, eye, eye, 1.3f, 0.03f);
                    yield return Move(p, at + fwd * 0.6f, at + fwd * 0.6f, yawT, yawT + 180, 0, 0, eye, eye, 1.0f);
                    Sfx.Play2D("SFX/sfx_elevator_ding", 0.8f);
                    gm.StartCoroutine(gm.UI.Fade(1f, 1.5f));
                    yield return new WaitForSeconds(1.5f);
                    break;
            }
            // leave the player frozen: the level is about to change
        }

        // ------------------------------------------------------------------ arrivals
        public static IEnumerator Arrive(ArrivalKind kind, Transform spawn)
        {
            var p = P;
            if (p == null || kind == ArrivalKind.None) yield break;
            Begin(p);
            var gm = GameManager.Instance;
            Vector3 s = spawn ? spawn.position : p.transform.position;
            float yaw = spawn ? spawn.eulerAngles.y : p.Yaw;
            Vector3 fwd = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
            float eye = p.EyeHeight;
            switch (kind)
            {
                case ArrivalKind.WakeUp:
                    // lying on the floor, blinking, then getting up
                    p.transform.SetPositionAndRotation(s, Quaternion.Euler(0, yaw, 0));
                    yield return Move(p, s, s, yaw, yaw, -60, -60, 0.25f, 0.25f, 0.05f, 0, 75, 75);
                    yield return new WaitForSeconds(1.2f);
                    for (int i = 0; i < 2; i++)
                    {
                        yield return gm.UI.Fade(0.85f, 0.25f);
                        yield return gm.UI.Fade(0.1f, 0.5f);
                    }
                    Sfx.Play2D("SFX/sfx_breath", 0.5f);
                    yield return Move(p, s, s, yaw, yaw, -60, -10, 0.25f, 0.9f, 1.6f, 0, 75, 10);
                    yield return Move(p, s, s, yaw, yaw, -10, 0, 0.9f, eye, 1.2f, 0, 10, 0);
                    break;
                case ArrivalKind.Elevator:
                {
                    // inside the cab: ride, ding, the scissor gate folds, step out
                    p.transform.SetPositionAndRotation(s, Quaternion.Euler(0, yaw, 0));
                    var rumble = Sfx.Random("SFX/sfx_elevator_move");
                    var src = p.GetComponentInChildren<AudioListener>().gameObject.AddComponent<AudioSource>();
                    if (rumble) { src.clip = rumble; src.volume = 0.8f; src.Play(); }
                    for (float t = 0; t < 4.5f; t += Time.deltaTime)
                    {
                        float shake = Mathf.PerlinNoise(t * 9f, 0) - 0.5f;
                        p.cameraPivot.localPosition = new Vector3(0, eye + shake * 0.012f, 0);
                        p.cameraPivot.localRotation = Quaternion.Euler(shake * 0.4f, 0, shake * 0.3f);
                        yield return null;
                    }
                    src.Stop();
                    Object.Destroy(src, 0.1f);
                    Sfx.Play2D("SFX/sfx_elevator_ding", 0.9f);
                    yield return new WaitForSeconds(0.8f);
                    var gate = GameObject.Find("ElevatorGate");
                    if (gate)
                    {
                        Sfx.PlayAt("SFX/sfx_door_open", gate.transform.position, 0.8f);
                        Vector3 sc = gate.transform.localScale;
                        for (float t = 0; t < 1.6f; t += Time.deltaTime)
                        {
                            gate.transform.localScale = new Vector3(Mathf.Lerp(sc.x, 0.08f, Ease(t / 1.6f)), sc.y, sc.z);
                            yield return null;
                        }
                        foreach (var c in gate.GetComponentsInChildren<Collider>()) c.enabled = false;
                    }
                    yield return Move(p, s, s + fwd * 1.6f, yaw, yaw, 0, 0, eye, eye, 1.6f, 0.03f);
                    break;
                }
                case ArrivalKind.FallIn:
                    Sfx.Play2D("SFX/sfx_door_open", 0.5f);
                    yield return Move(p, s + Vector3.up * 2.2f - fwd * 0.8f, s, yaw, yaw, 55, 40, 0.6f, 0.35f, 0.55f, 0, -20, 35);
                    Sfx.Play2D("Footsteps/fs_grass", 1f);
                    gm.UI.Hud.Flash(new Color(1, 1, 1, 0.25f));
                    yield return new WaitForSeconds(0.9f);
                    yield return Move(p, s, s, yaw, yaw, 40, 0, 0.35f, eye, 1.8f, 0, 35, 0);
                    break;
                case ArrivalKind.Emerge:
                    Sfx.Play2D("SFX/sfx_underwater", 0.7f, 0);
                    yield return Move(p, s + Vector3.down * 1.6f, s + Vector3.down * 1.0f, yaw, yaw, -50, -35, eye, eye, 1.5f);
                    Sfx.Play2D("Footsteps/fs_water", 1f);
                    Sfx.Play2D("SFX/sfx_breath", 0.7f);
                    yield return Move(p, s + Vector3.down * 1.0f, s, yaw, yaw, -35, 0, eye, eye, 1.4f);
                    break;
                case ArrivalKind.Stairs:
                    yield return Move(p, s - fwd * 3f + Vector3.down * 1.6f, s, yaw, yaw, -15, 0, eye, eye, 2.4f, 0.05f);
                    break;
                case ArrivalKind.Walk:
                    yield return Move(p, s - fwd * 2f, s, yaw, yaw, 0, 0, eye, eye, 1.6f, 0.03f);
                    break;
                case ArrivalKind.Ladder:
                    for (int i = 3; i > 0; i--)
                    {
                        Sfx.Play2D("Footsteps/fs_concrete", 0.5f);
                        var a = s + Vector3.down * (i * 0.6f) - fwd * 0.3f;
                        yield return Move(p, a, a + Vector3.up * 0.6f, yaw + 180, yaw + 180, -20, -10, eye, eye, 0.5f);
                    }
                    yield return Move(p, s - fwd * 0.3f, s + fwd * 0.4f, yaw + 180, yaw, 0, 0, eye, eye, 1.2f);
                    break;
            }
            End(p);
        }
    }
}
