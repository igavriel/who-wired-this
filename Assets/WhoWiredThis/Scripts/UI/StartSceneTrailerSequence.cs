using System.Collections;
using UnityEngine;

namespace WhoWiredThis.UI
{
    /// <summary>
    /// StartScene flow: show name + instructions for a delay, play the looping trailer
    /// for a fixed duration, then restore name + instructions.
    /// </summary>
    [DisallowMultipleComponent]
    public class StartSceneTrailerSequence : MonoBehaviour
    {
        private const string LogPrefix = "[StartSceneTrailerSequence]";

        [SerializeField]
        private YoutubeWebViewController youtube;

        [Tooltip("Image-Name and UI_PopupMessagePanel_PerPlayer on both start canvases.")]
        [SerializeField]
        private GameObject[] introUiRoots;

        [SerializeField]
        private float introSeconds = 30f;

        [Tooltip("How long the looping trailer plays before stopping (e.g. 56 for 0:56).")]
        [SerializeField]
        private float videoPlaySeconds = 56f;

        private Coroutine sequenceRoutine;

        private void OnEnable()
        {
            if (youtube == null)
            {
                youtube = GetComponent<YoutubeWebViewController>();
            }

            sequenceRoutine = StartCoroutine(RunSequence());
        }

        private void OnDisable()
        {
            if (sequenceRoutine != null)
            {
                StopCoroutine(sequenceRoutine);
                sequenceRoutine = null;
            }
        }

        private IEnumerator RunSequence()
        {
            // Phase 1 — show title + instructions; video surfaces hidden.
            SetIntroUiVisible(true);
            if (youtube != null)
            {
                youtube.StopPlayback();
            }

            Debug.Log($"{LogPrefix} Intro UI for {introSeconds:0.#}s.", this);
            if (introSeconds > 0f)
            {
                yield return new WaitForSecondsRealtime(introSeconds);
            }

            if (!isActiveAndEnabled)
            {
                yield break;
            }

            // Phase 2 — hide intro UI; play looping trailer for a fixed duration.
            SetIntroUiVisible(false);
            if (youtube == null)
            {
                Debug.LogWarning($"{LogPrefix} YoutubeWebViewController not assigned.", this);
                yield break;
            }

            youtube.StartPlayback();
            float playSeconds = Mathf.Max(0.1f, videoPlaySeconds);
            Debug.Log($"{LogPrefix} Trailer looping for {playSeconds:0.#}s.", this);
            yield return new WaitForSecondsRealtime(playSeconds);

            if (!isActiveAndEnabled)
            {
                yield break;
            }

            // Phase 3 — stop video/audio (hides anchors); show instructions + name again.
            youtube.StopPlayback();
            SetIntroUiVisible(true);
            Debug.Log($"{LogPrefix} Trailer window elapsed; intro UI restored.", this);
            sequenceRoutine = null;
        }

        private void SetIntroUiVisible(bool visible)
        {
            if (introUiRoots == null)
            {
                return;
            }

            for (int i = 0; i < introUiRoots.Length; i++)
            {
                if (introUiRoots[i] != null)
                {
                    introUiRoots[i].SetActive(visible);
                }
            }
        }
    }
}
