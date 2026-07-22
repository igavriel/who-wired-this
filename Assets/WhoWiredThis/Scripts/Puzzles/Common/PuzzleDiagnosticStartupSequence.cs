using System.Collections;
using UnityEngine;
using WhoWiredThis.Enums;
using WhoWiredThis.Scenes;
using WhoWiredThis.Visibility;

namespace WhoWiredThis.Puzzles.Common
{
    /// <summary>
    /// Dual-surface startup for Pipes/Signal: SceneStageManager writes role intro to the local
    /// Rules panel; after a hold, this component writes standby copy to the local Monitor.
    /// Reader monitors get a 40×12 puzzle log standby; operator monitors get short idle copy.
    /// Cancels if the local puzzle receives a submit before the standby step.
    /// </summary>
    public class PuzzleDiagnosticStartupSequence : MonoBehaviour
    {
        [Header("Panel identity")]
        [Tooltip("When true, this panel is Player A's board (Player B when false).")]
        [SerializeField] private bool isPlayerAPanel = true;

        [Header("References")]
        [SerializeField] private DiagnosticDisplayController localMonitor;

        [SerializeField] private MultiDimensionPuzzleManager localPuzzleManager;

        [Tooltip("Partner panel puzzle manager — cancels standby when the operator submits.")]
        [SerializeField] private MultiDimensionPuzzleManager partnerPuzzleManager;

        [Tooltip("Pipes operator adapter on either panel (provides BuildStandbyBody for reader monitor).")]
        [SerializeField] private ComponentDiagnosticAdapter pipesStandbySource;

        [Tooltip("Signal operator adapter on either panel (provides BuildStandbyBody for reader monitor).")]
        [SerializeField] private SignalDiagnosticAdapter signalStandbySource;

        [Header("Timing")]
        [SerializeField] private float introHoldSeconds = 4f;

        [Header("Operator monitor copy (local panel is operating this stage)")]
        [SerializeField]
        [TextArea(3, 6)]
        private string operatorMonitorBody =
            "PARTNER READS THEIR MONITOR.\n\nADJUST CONTROLS AND PRESS SEND.";

        private SceneStageManager stageManager;
        private Coroutine sequenceCoroutine;
        private bool attemptSeen;

        private void OnEnable()
        {
            if (localPuzzleManager != null)
            {
                localPuzzleManager.OnAttemptSubmitted += HandleAnyAttempt;
            }

            if (partnerPuzzleManager != null)
            {
                partnerPuzzleManager.OnAttemptSubmitted += HandleAnyAttempt;
            }

            stageManager = FindFirstObjectByType<SceneStageManager>();
            if (stageManager != null)
            {
                stageManager.OnStageChanged += HandleStageChanged;
            }

            RestartSequence();
        }

        private void OnDisable()
        {
            if (localPuzzleManager != null)
            {
                localPuzzleManager.OnAttemptSubmitted -= HandleAnyAttempt;
            }

            if (partnerPuzzleManager != null)
            {
                partnerPuzzleManager.OnAttemptSubmitted -= HandleAnyAttempt;
            }

            if (stageManager != null)
            {
                stageManager.OnStageChanged -= HandleStageChanged;
            }

            if (sequenceCoroutine != null)
            {
                StopCoroutine(sequenceCoroutine);
                sequenceCoroutine = null;
            }
        }

        private void HandleStageChanged(SceneSessionStage _)
        {
            RestartSequence();
        }

        private void HandleAnyAttempt(MultiDimensionAttemptResult result)
        {
            if (result == null)
            {
                return;
            }

            attemptSeen = true;
            if (sequenceCoroutine != null)
            {
                StopCoroutine(sequenceCoroutine);
                sequenceCoroutine = null;
            }
        }

        private void RestartSequence()
        {
            if (sequenceCoroutine != null)
            {
                StopCoroutine(sequenceCoroutine);
            }

            attemptSeen = false;
            sequenceCoroutine = StartCoroutine(RunStandbyAfterIntroHold());
        }

        private IEnumerator RunStandbyAfterIntroHold()
        {
            if (localMonitor == null)
            {
                yield break;
            }

            if (localPuzzleManager != null && localPuzzleManager.Solved)
            {
                yield break;
            }

            // Let SceneStageManager apply Rules-panel intro on the next frame first.
            yield return null;

            if (introHoldSeconds > 0f)
            {
                yield return new WaitForSeconds(introHoldSeconds);
            }

            if (attemptSeen || localMonitor == null)
            {
                yield break;
            }

            if (localPuzzleManager != null && localPuzzleManager.Solved)
            {
                yield break;
            }

            string body = BuildStandbyBodyForCurrentStage();
            if (!string.IsNullOrEmpty(body))
            {
                localMonitor.SetInstructionBody(body);
            }
        }

        private string BuildStandbyBodyForCurrentStage()
        {
            if (IsLocalPanelOperator())
            {
                return operatorMonitorBody ?? string.Empty;
            }

            if (pipesStandbySource != null)
            {
                return pipesStandbySource.BuildStandbyBody();
            }

            if (signalStandbySource != null)
            {
                return signalStandbySource.BuildStandbyBody();
            }

            return string.Empty;
        }

        private bool IsLocalPanelOperator()
        {
            if (stageManager == null)
            {
                return isPlayerAPanel;
            }

            SceneSessionStage stage = stageManager.CurrentStage;
            if (stage == SceneSessionStage.Complete)
            {
                return false;
            }

            bool playerAOperates = stage == SceneSessionStage.PlayerAOperator;
            return isPlayerAPanel ? playerAOperates : !playerAOperates;
        }
    }
}
