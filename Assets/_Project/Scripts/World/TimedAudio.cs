using UnityEngine;

namespace SyncRooms.World
{
    /// <summary>Plays its AudioSource periodically (siren poles, gramophones switching records...).</summary>
    [RequireComponent(typeof(AudioSource))]
    public class TimedAudio : MonoBehaviour
    {
        public AudioClip[] clips;
        public float minGap = 20f, maxGap = 60f;
        public bool startPlaying = true;

        AudioSource _src;
        float _timer;

        void Start()
        {
            _src = GetComponent<AudioSource>();
            _timer = startPlaying ? Random.Range(0f, 3f) : Random.Range(minGap, maxGap);
        }

        void Update()
        {
            if (_src.isPlaying || clips == null || clips.Length == 0) return;
            _timer -= Time.deltaTime;
            if (_timer > 0f) return;
            _src.clip = clips[Random.Range(0, clips.Length)];
            _src.Play();
            _timer = Random.Range(minGap, maxGap);
        }
    }
}
