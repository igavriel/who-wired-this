using System;
using UnityEngine;

namespace WhoWiredThis.Visibility
{
    [Serializable]
    public class MultiDimensionSwitchAudioSettings
    {
        [SerializeField]
        private bool enabled = true;

        [SerializeField]
        private AudioSource audioSource;

        [SerializeField]
        private AudioClip[] clips = Array.Empty<AudioClip>();

        [SerializeField]
        [Range(0.5f, 2f)]
        private float pitchMin = 0.94f;

        [SerializeField]
        [Range(0.5f, 2f)]
        private float pitchMax = 1.06f;

        [SerializeField]
        [Range(0f, 1f)]
        private float volumeMin = 0.88f;

        [SerializeField]
        [Range(0f, 1f)]
        private float volumeMax = 1f;

        public bool Enabled => enabled;
        public AudioSource AudioSource => audioSource;
        public AudioClip[] Clips => clips;
        public float PitchMin => pitchMin;
        public float PitchMax => pitchMax;
        public float VolumeMin => volumeMin;
        public float VolumeMax => volumeMax;
    }
}
