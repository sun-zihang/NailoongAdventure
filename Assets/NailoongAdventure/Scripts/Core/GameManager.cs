using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nailoong
{
    public enum GameState { Boot, MainMenu, Playing, Paused, LevelClear, GameOver, Ending }

    /// <summary>
    /// 全局流程控制：状态机 + 关卡切换 + 存档 + 暂停。
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("关卡场景名（需与 Build Settings 一致）")]
        public string[] levelScenes = { "Level1_Beach", "Level2_Forest", "Level3_Volcano" };
        public string menuScene = "MainMenu";

        public GameState State { get; private set; } = GameState.Boot;
        public int CurrentLevel { get; private set; } = 0;
        public float LevelTime { get; private set; }
        public bool IsBusy { get; private set; }

        // 存档数据
        public SaveData Save { get; private set; } = new SaveData();

        const string SAVE_KEY = "NailoongAdventure.Save.v1";

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Application.targetFrameRate = 60;
            LoadSave();
        }

        void OnEnable() => SceneManager.sceneLoaded += HandleSceneLoaded;
        void OnDisable() => SceneManager.sceneLoaded -= HandleSceneLoaded;

        void Update()
        {
            if (State == GameState.Playing && !IsBusy)
                LevelTime += Time.deltaTime;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (State == GameState.Playing) Pause();
                else if (State == GameState.Paused) Resume();
            }
        }

        void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            GameEvents.ResetAll();
            IsBusy = false;
            LevelTime = 0f;
            Time.timeScale = 1f;

            var flow = FindObjectOfType<LevelFlow>();
            if (flow != null)
            {
                CurrentLevel = flow.levelIndex;
                State = GameState.Playing;
                GameEvents.LevelStart(CurrentLevel);
            }
            else if (scene.name == menuScene)
            {
                State = GameState.MainMenu;
            }
            else
            {
                State = GameState.Playing;
            }
        }

        // ---------- 流程 ----------
        public void StartNewGame()
        {
            Save = new SaveData();
            WriteSave();
            LoadLevel(0);
        }

        public void ContinueGame() => LoadLevel(Mathf.Clamp(Save.clearedLevels, 0, levelScenes.Length - 1));

        public void LoadLevel(int index)
        {
            if (IsBusy) return;
            if (index < 0 || index >= levelScenes.Length) { ToMenu(); return; }
            StartCoroutine(LoadSceneRoutine(levelScenes[index]));
        }

        public void ReloadLevel() => StartCoroutine(LoadSceneRoutine(SceneManager.GetActiveScene().name));

        public void NextLevel()
        {
            if (CurrentLevel + 1 < levelScenes.Length) LoadLevel(CurrentLevel + 1);
            else ToEnding();
        }

        public void ToEnding() => StartCoroutine(LoadSceneRoutine("Ending"));
        public void ToMenu() => StartCoroutine(LoadSceneRoutine(menuScene));

        IEnumerator LoadSceneRoutine(string sceneName)
        {
            IsBusy = true;
            Time.timeScale = 1f;
            yield return new WaitForEndOfFrame();
            var op = SceneManager.LoadSceneAsync(sceneName);
            if (op == null)
            {
                Debug.LogError($"[GameManager] 场景 {sceneName} 不存在，请确认已加入 Build Settings。");
                IsBusy = false;
                yield break;
            }
            while (!op.isDone) yield return null;
            IsBusy = false;
        }

        public void ClearCurrentLevel()
        {
            if (State != GameState.Playing) return;
            State = GameState.LevelClear;
            Save.clearedLevels = Mathf.Max(Save.clearedLevels, CurrentLevel + 1);
            Save.bestTime[CurrentLevel] = Save.bestTime.ContainsKey(CurrentLevel)
                ? Mathf.Min(Save.bestTime[CurrentLevel], LevelTime)
                : LevelTime;
            WriteSave();
            GameEvents.LevelClear(CurrentLevel, LevelTime);
        }

        public void GameOver()
        {
            if (State == GameState.GameOver) return;
            State = GameState.GameOver;
            GameEvents.GameOver();
        }

        public void Pause()
        {
            if (State != GameState.Playing) return;
            State = GameState.Paused;
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void Resume()
        {
            if (State != GameState.Paused) return;
            State = GameState.Playing;
            Time.timeScale = 1f;
#if UNITY_WEBGL
            // WebGL 上保持鼠标可见（同 CameraRig 的说明）
            Cursor.visible = true;
#else
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
#endif
        }

        public void Quit()
        {
            WriteSave();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ---------- 存档 ----------
        public void UnlockSkill(string id)
        {
            if (Save.unlockedSkills.Contains(id)) return;
            Save.unlockedSkills.Add(id);
            WriteSave();
        }

        public bool HasSkill(string id) => Save.unlockedSkills.Contains(id);

        void LoadSave()
        {
            if (PlayerPrefs.HasKey(SAVE_KEY))
            {
                try { Save = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(SAVE_KEY)); }
                catch { Save = new SaveData(); }
            }
            if (Save == null) Save = new SaveData();
            Save.Ensure();
        }

        public void WriteSave()
        {
            Save.Ensure();
            PlayerPrefs.SetString(SAVE_KEY, JsonUtility.ToJson(Save));
            PlayerPrefs.Save();
        }
    }

    [System.Serializable]
    public class SaveData
    {
        public int clearedLevels;
        public System.Collections.Generic.List<string> unlockedSkills = new System.Collections.Generic.List<string>();
        public System.Collections.Generic.List<int> bestTimeKeys = new System.Collections.Generic.List<int>();
        public System.Collections.Generic.List<float> bestTimeValues = new System.Collections.Generic.List<float>();

        [System.NonSerialized]
        public System.Collections.Generic.Dictionary<int, float> bestTime = new System.Collections.Generic.Dictionary<int, float>();

        public void Ensure()
        {
            if (unlockedSkills == null) unlockedSkills = new System.Collections.Generic.List<string>();
            if (bestTimeKeys == null) bestTimeKeys = new System.Collections.Generic.List<int>();
            if (bestTimeValues == null) bestTimeValues = new System.Collections.Generic.List<float>();
            if (bestTime == null) bestTime = new System.Collections.Generic.Dictionary<int, float>();
        }
    }
}
