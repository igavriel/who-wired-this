using System.Collections;
using UnityEngine;

namespace WhoWiredThis.Visibility
{
    /// <summary>
    /// Drives a 2-state submit lever MultiDimension: ON at submit, OFF after delay on failure, latched ON on success.
    /// </summary>
    public class SubmitLeverMultiDimensionFeedback : MonoBehaviour
    {
        [SerializeField]
        private MultiDimension lever;

        [SerializeField]
        private int onSubjectIndex = 1;

        [SerializeField]
        private int offSubjectIndex = 0;

        [SerializeField]
        [Min(0f)]
        private float revertDelaySeconds = 1f;

        public void SetSubmitOn()
        {
            if (lever == null)
            {
                return;
            }

            lever.SetActiveSubjectIndex(onSubjectIndex);
        }

        public IEnumerator FinishSubmitRoutine(bool puzzleSolved)
        {
            if (lever == null)
            {
                yield break;
            }

            if (puzzleSolved)
            {
                yield break;
            }

            if (revertDelaySeconds > 0f)
            {
                yield return new WaitForSeconds(revertDelaySeconds);
            }

            lever.SetActiveSubjectIndex(offSubjectIndex);
        }
    }
}
