using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace SyncRooms.Gen
{
    /// <summary>A reflection probe that follows the camera and re-renders periodically (for streamed levels).</summary>
    [RequireComponent(typeof(HDAdditionalReflectionData))]
    public class FollowProbe : MonoBehaviour
    {
        public float interval = 0.6f;
        public float moveThreshold = 3f;
        HDAdditionalReflectionData _probe;
        Vector3 _last;
        float _t;

        void Start()
        {
            _probe = GetComponent<HDAdditionalReflectionData>();
            _probe.mode = ProbeSettings.Mode.Realtime;
            _probe.realtimeMode = ProbeSettings.RealtimeMode.OnDemand;
            _probe.RequestRenderNextUpdate();
        }

        void LateUpdate()
        {
            var cam = Camera.main;
            if (!cam) return;
            transform.position = cam.transform.position;
            _t += Time.deltaTime;
            if (_t > interval && (transform.position - _last).sqrMagnitude > moveThreshold * moveThreshold)
            {
                _t = 0;
                _last = transform.position;
                _probe.RequestRenderNextUpdate();
            }
        }

        public static void Create(Transform parent, Vector3 size)
        {
            var go = new GameObject("Follow Probe");
            go.transform.SetParent(parent, false);
            go.AddComponent<ReflectionProbe>();
            var hd = go.AddComponent<HDAdditionalReflectionData>();
            hd.influenceVolume.shape = InfluenceShape.Box;
            hd.influenceVolume.boxSize = size;
            hd.influenceVolume.boxBlendDistancePositive = Vector3.one * 2;
            hd.influenceVolume.boxBlendDistanceNegative = Vector3.one * 2;
            go.AddComponent<FollowProbe>();
        }
    }
}
