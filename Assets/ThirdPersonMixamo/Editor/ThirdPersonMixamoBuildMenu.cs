#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ThirdPersonMixamo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ThirdPersonMixamo.Editor
{
    public static class ThirdPersonMixamoBuildMenu
    {
        private const string PrefabPath = "Assets/ThirdPersonMixamo/Prefabs/ThirdPersonMixamoPlayer.prefab";
        private const string AstraPrefabPath = "Assets/Mixamo/astra-prefab.prefab";
        private const string AnimatorPath = "Assets/ThirdPersonMixamo/Animations/ThirdPersonMixamo_StarterThirdPerson.controller";
        private const string BindingsAPath = "Assets/ThirdPersonMixamo/Data/PlayerControlBindings_PlayerA.asset";
        private const string BindingsBPath = "Assets/ThirdPersonMixamo/Data/PlayerControlBindings_PlayerB.asset";
        private const string FootstepPath = "Assets/ThirdPersonMixamo/Audio/Player_Footstep_01.wav";
        private const string LandPath = "Assets/ThirdPersonMixamo/Audio/Player_Land.wav";
        private const string SingleScenePath = "Assets/ThirdPersonMixamo/ThirdPersonMixamo_Single.unity";
        private const string DuelScenePath = "Assets/ThirdPersonMixamo/ThirdPersonMixamo_LocalDuel.unity";

        [MenuItem("ThirdPersonMixamo/Rebuild Package Assets (Prefab + Scenes)")]
        public static void RebuildPackageAssets()
        {
            BuildPlayerPrefab();
            BuildSingleScene();
            BuildDuelScene();
            RegisterBuildScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ThirdPersonMixamo] Rebuild complete.");
        }

        private static void RegisterBuildScenes()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            void AddIfMissing(string path)
            {
                if (scenes.Any(s => s.path == path))
                {
                    return;
                }

                scenes.Add(new EditorBuildSettingsScene(path, true));
            }

            AddIfMissing(SingleScenePath);
            AddIfMissing(DuelScenePath);
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void BuildPlayerPrefab()
        {
            var root = new GameObject("ThirdPersonMixamoPlayer");
            var cc = root.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.stepOffset = 0.3f;

            var player = root.AddComponent<PlayerController>();
            root.AddComponent<ThirdPersonAnimatorBridge>();
            var audio = root.AddComponent<ThirdPersonPlayerAudio>();

            var astraPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AstraPrefabPath);
            if (astraPrefab == null)
            {
                Debug.LogError("[ThirdPersonMixamo] Missing astra prefab at " + AstraPrefabPath);
            }
            else
            {
                var astra = (GameObject)PrefabUtility.InstantiatePrefab(astraPrefab, root.transform);
                astra.transform.localPosition = Vector3.zero;
                astra.transform.localRotation = Quaternion.identity;
                astra.transform.localScale = Vector3.one;

                var anim = root.GetComponentInChildren<Animator>();
                var ac = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AnimatorPath);
                if (anim != null && ac != null)
                {
                    anim.runtimeAnimatorController = ac;
                }
            }

            var bindingsA = AssetDatabase.LoadAssetAtPath<PlayerControlBindings>(BindingsAPath);
            var soPlayer = new SerializedObject(player);
            soPlayer.FindProperty("inputBindings").objectReferenceValue = bindingsA;
            soPlayer.ApplyModifiedPropertiesWithoutUndo();

            var foot = AssetDatabase.LoadAssetAtPath<AudioClip>(FootstepPath);
            var land = AssetDatabase.LoadAssetAtPath<AudioClip>(LandPath);
            if (foot == null || land == null)
            {
                Debug.LogError("[ThirdPersonMixamo] Missing Audio clips at " + FootstepPath + " / " + LandPath);
            }

            var soAudio = new SerializedObject(audio);
            var pFoot = soAudio.FindProperty("footstepClip");
            var pLand = soAudio.FindProperty("landClip");
            if (pFoot != null && pLand != null)
            {
                pFoot.objectReferenceValue = foot;
                pLand.objectReferenceValue = land;
                soAudio.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                Debug.LogError("[ThirdPersonMixamo] SerializedProperty footstepClip/landClip not found on ThirdPersonPlayerAudio.");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath) ?? "Assets");
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
        }

        private static void BuildSingleScene()
        {
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var scene = SceneManager.GetActiveScene();

            var oldCam = GameObject.Find("Main Camera");
            if (oldCam != null)
            {
                Object.DestroyImmediate(oldCam);
            }

            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            ground.transform.position = new Vector3(0f, -0.5f, 0f);
            ground.transform.localScale = new Vector3(40f, 1f, 40f);

            AddJumpGym(ground.transform.position.y + 0.5f);

            var camGo = new GameObject("PlayerCamera");
            camGo.tag = "MainCamera";
            camGo.transform.position = new Vector3(0f, 2.5f, -6f);
            var cam = camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<PlayerCameraRig>();

            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            player.transform.position = new Vector3(0f, 0f, 0f);

            WirePlayerAndCamera(player, camGo);

            EditorSceneManager.SaveScene(scene, SingleScenePath);
        }

        private static void BuildDuelScene()
        {
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var scene = SceneManager.GetActiveScene();

            var oldCam = GameObject.Find("Main Camera");
            if (oldCam != null)
            {
                Object.DestroyImmediate(oldCam);
            }

            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            ground.transform.position = new Vector3(0f, -0.5f, 0f);
            ground.transform.localScale = new Vector3(50f, 1f, 40f);

            AddJumpGym(ground.transform.position.y + 0.5f);

            var camLeftGo = new GameObject("PlayerCamera_Left");
            camLeftGo.tag = "MainCamera";
            var camLeft = camLeftGo.AddComponent<Camera>();
            camLeftGo.AddComponent<AudioListener>();
            camLeftGo.AddComponent<PlayerCameraRig>();
            camLeft.rect = new Rect(0f, 0f, 0.5f, 1f);
            camLeftGo.transform.position = new Vector3(-4f, 2.5f, -6f);

            var camRightGo = new GameObject("PlayerCamera_Right");
            var camRight = camRightGo.AddComponent<Camera>();
            camRightGo.AddComponent<PlayerCameraRig>();
            var listener = camRightGo.GetComponent<AudioListener>();
            if (listener != null)
            {
                Object.DestroyImmediate(listener);
            }

            camRight.rect = new Rect(0.5f, 0f, 0.5f, 1f);
            camRightGo.transform.position = new Vector3(4f, 2.5f, -6f);

            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var p1 = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            p1.name = "Player_A";
            p1.transform.position = new Vector3(-3f, 0f, 0f);
            var p2 = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            p2.name = "Player_B";
            p2.transform.position = new Vector3(3f, 0f, 0f);

            var bindingsA = AssetDatabase.LoadAssetAtPath<PlayerControlBindings>(BindingsAPath);
            var bindingsB = AssetDatabase.LoadAssetAtPath<PlayerControlBindings>(BindingsBPath);

            var pc1 = p1.GetComponent<PlayerController>();
            var so1 = new SerializedObject(pc1);
            so1.FindProperty("inputBindings").objectReferenceValue = bindingsA;
            so1.FindProperty("cameraTransform").objectReferenceValue = camLeftGo.transform;
            so1.ApplyModifiedPropertiesWithoutUndo();

            var pc2 = p2.GetComponent<PlayerController>();
            var so2 = new SerializedObject(pc2);
            so2.FindProperty("inputBindings").objectReferenceValue = bindingsB;
            so2.FindProperty("cameraTransform").objectReferenceValue = camRightGo.transform;
            so2.ApplyModifiedPropertiesWithoutUndo();

            var rigLeft = camLeftGo.GetComponent<PlayerCameraRig>();
            var rigRight = camRightGo.GetComponent<PlayerCameraRig>();
            SetRigTarget(rigLeft, p1.transform);
            SetRigTarget(rigRight, p2.transform);

            EditorSceneManager.SaveScene(scene, DuelScenePath);
        }

        private static void SetRigTarget(PlayerCameraRig rig, Transform target)
        {
            var so = new SerializedObject(rig);
            so.FindProperty("target").objectReferenceValue = target;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WirePlayerAndCamera(GameObject player, GameObject camGo)
        {
            var pc = player.GetComponent<PlayerController>();
            var so = new SerializedObject(pc);
            so.FindProperty("cameraTransform").objectReferenceValue = camGo.transform;
            so.ApplyModifiedPropertiesWithoutUndo();

            var rig = camGo.GetComponent<PlayerCameraRig>();
            SetRigTarget(rig, player.transform);
        }

        private static void AddJumpGym(float groundY)
        {
            void Box(Vector3 pos, Vector3 scale, string name)
            {
                var b = GameObject.CreatePrimitive(PrimitiveType.Cube);
                b.name = name;
                b.transform.position = pos;
                b.transform.localScale = scale;
            }

            Box(new Vector3(4f, groundY + 0.5f, 2f), new Vector3(2f, 1f, 2f), "JumpBox_Low");
            Box(new Vector3(7f, groundY + 1.25f, 2f), new Vector3(2f, 1.5f, 2f), "JumpBox_Mid");
            Box(new Vector3(10.5f, groundY + 2f, 2f), new Vector3(2f, 2f, 2f), "JumpBox_High");
            Box(new Vector3(6f, groundY + 0.5f, 6f), new Vector3(1.5f, 1f, 8f), "JumpBox_Platform");
            Box(new Vector3(14f, groundY + 0.75f, -1f), new Vector3(2.5f, 1.5f, 2.5f), "JumpBox_Far");
        }
    }
}
#endif
