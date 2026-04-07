using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using WhoWiredThis.UI;

namespace WhoWiredThis.Editor
{
    /// <summary>
    /// Run once via  WhoWiredThis → Setup Help Panel  to add the HelpPanel
    /// UI to the open POC_Main scene and wire it to HUDController.helpPanel.
    /// Safe to re-run — it removes the old panel first.
    /// </summary>
    public static class SetupHelpPanel
    {
        private const string HelpText =
            "<b>CONTROLS</b>\n" +
            "──────────────────────────\n" +
            "<b>WASD / Arrows</b>      Move\n" +
            "<b>Right-Click + Drag</b>  Rotate Camera\n" +
            "<b>E</b>                  Interact\n" +
            "<b>1 / 2 / 3</b>          Select Inventory Slot\n" +
            "<b>I</b>  or  [BAG]       Toggle Inventory\n" +
            "<b>Space / Enter</b>       Close Popup\n" +
            "<b>H</b>                  Toggle This Help";

        [MenuItem("WhoWiredThis/Setup Help Panel")]
        public static void Run()
        {
            var canvasGO = GameObject.Find("UI_Canvas");
            if (canvasGO == null)
            {
                Debug.LogError("[WWI] UI_Canvas not found — open POC_Main first.");
                return;
            }

            // Remove stale panel if it exists
            var old = canvasGO.transform.Find("HelpPanel");
            if (old != null)
            {
                Object.DestroyImmediate(old.gameObject);
            }

            // ── Panel root ──────────────────────────────────────
            var panel = new GameObject("HelpPanel");
            panel.transform.SetParent(canvasGO.transform, false);

            var bg = panel.AddComponent<Image>();
            bg.color = new Color(0.04f, 0.05f, 0.08f, 0.95f);

            var rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.25f, 0.25f);
            rt.anchorMax = new Vector2(0.75f, 0.78f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            // ── Help text ────────────────────────────────────────
            var textGO = new GameObject("HelpText");
            textGO.transform.SetParent(panel.transform, false);

            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text = HelpText;
            tmp.fontSize = 17;
            tmp.color = new Color(0.92f, 0.92f, 0.88f, 1f);
            tmp.alignment = TextAlignmentOptions.TopLeft;
            tmp.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
            tmp.richText = true;
            tmp.margin = new Vector4(20f, 16f, 20f, 48f);

            var txtRT = textGO.GetComponent<RectTransform>();
            txtRT.anchorMin = Vector2.zero;
            txtRT.anchorMax = Vector2.one;
            txtRT.offsetMin = txtRT.offsetMax = Vector2.zero;

            // ── [H] close button ─────────────────────────────────
            var btnGO = new GameObject("CloseButton");
            btnGO.transform.SetParent(panel.transform, false);

            var btnImg = btnGO.AddComponent<Image>();
            btnImg.color = new Color(0.15f, 0.15f, 0.18f, 0.9f);

            var btn = btnGO.AddComponent<Button>();
            var btnRT = btnGO.GetComponent<RectTransform>();
            btnRT.anchorMin = new Vector2(0.3f, 0.0f);
            btnRT.anchorMax = new Vector2(0.7f, 0.18f);
            btnRT.offsetMin = new Vector2(0f, 4f);
            btnRT.offsetMax = new Vector2(0f, -4f);

            var lblGO = new GameObject("Label");
            lblGO.transform.SetParent(btnGO.transform, false);

            var lbl = lblGO.AddComponent<TextMeshProUGUI>();
            lbl.text = "[ H ]  Close Help";
            lbl.fontSize = 15;
            lbl.color = new Color(0.7f, 0.9f, 0.7f, 1f);
            lbl.alignment = TextAlignmentOptions.Midline;

            var lblRT = lblGO.GetComponent<RectTransform>();
            lblRT.anchorMin = Vector2.zero;
            lblRT.anchorMax = Vector2.one;
            lblRT.offsetMin = lblRT.offsetMax = Vector2.zero;

            panel.SetActive(false);

            // ── Wire HUDController.helpPanel ─────────────────────
            var hud = canvasGO.GetComponent<HUDController>();
            if (hud != null)
            {
                hud.helpPanel = panel;
                EditorUtility.SetDirty(canvasGO);
            }
            else
            {
                Debug.LogWarning("[WWI] HUDController not found on UI_Canvas — assign helpPanel manually.");
            }

            // Button onClick is wired at runtime in HUDController.Start()
            // for pre-assigned panels (see WireHelpPanelCloseButton).

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

            Debug.Log("[WWI] HelpPanel created and wired. Press H in-game to toggle.");
        }
    }
}
