using UnityEngine;

namespace WhoWiredThis.Visibility
{
    /// <summary>
    /// Plays optional switch-change audio for <see cref="MultiDimension"/> when the player advances a subject index.
    /// </summary>
    public static class MultiDimensionSwitchAudioPlayer
    {
        public static bool TryPlay(MultiDimensionSwitchAudioSettings settings, Component owner)
        {
            if (settings == null || !settings.Enabled || owner == null)
            {
                return false;
            }

            AudioClip clip = PickRandomClip(settings.Clips);
            if (clip == null)
            {
                return false;
            }

            AudioSource source = ResolveAudioSource(settings, owner);
            if (source == null)
            {
                return false;
            }

            float pitchMin = Mathf.Min(settings.PitchMin, settings.PitchMax);
            float pitchMax = Mathf.Max(settings.PitchMin, settings.PitchMax);
            float volumeMin = Mathf.Min(settings.VolumeMin, settings.VolumeMax);
            float volumeMax = Mathf.Max(settings.VolumeMin, settings.VolumeMax);

            float previousPitch = source.pitch;
            source.pitch = Random.Range(pitchMin, pitchMax);
            source.PlayOneShot(clip, Random.Range(volumeMin, volumeMax));
            source.pitch = previousPitch;
            return true;
        }

        public static bool HasPlayableClips(MultiDimensionSwitchAudioSettings settings)
        {
            return settings != null && settings.Enabled && PickRandomClip(settings.Clips) != null;
        }

        public static bool CanResolveAudioSource(MultiDimensionSwitchAudioSettings settings, Component owner)
        {
            return ResolveAudioSource(settings, owner) != null;
        }

        private static AudioClip PickRandomClip(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0)
            {
                return null;
            }

            int validCount = 0;
            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] != null)
                {
                    validCount++;
                }
            }

            if (validCount == 0)
            {
                return null;
            }

            int pick = Random.Range(0, validCount);
            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] == null)
                {
                    continue;
                }

                if (pick == 0)
                {
                    return clips[i];
                }

                pick--;
            }

            return null;
        }

        private static AudioSource ResolveAudioSource(MultiDimensionSwitchAudioSettings settings, Component owner)
        {
            if (settings.AudioSource != null)
            {
                return settings.AudioSource;
            }

            return owner.GetComponent<AudioSource>();
        }
    }
}
