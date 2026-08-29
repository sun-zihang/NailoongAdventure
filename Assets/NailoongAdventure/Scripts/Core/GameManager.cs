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
            else if (scene.name == "Ending")
            {
                State = GameState.Ending;
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

        public void ToEnding()
        {
            // Ending 场景不存在时兜底回主菜单，避免黑屏报错
            if (SceneUtility.GetBuildIndexByScenePath("Ending") < 0) { ToMenu(); return; }
            StartCoroutine(LoadSceneRoutine("Ending"));
        }
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

            // 星级评分：parTime 内 3 星，1.5 倍内 2 星，否则 1 星
            int stars = 1;
            var flow = FindObjectOfType<LevelFlow>();
            if (flow != null)
            {
                float par = flow.ParTime;
                if (LevelTime <= par) stars = 3;
                else if (LevelTime <= par * 1.5f) stars = 2;
            }
            int prev = GetStars(CurrentLevel);
            Save.bestStars[CurrentLevel] = Mathf.Max(prev, stars);

            WriteSave();
            GameEvents.LevelClear(CurrentLevel, LevelTime);
        }

        public int GetStars(int levelIndex) => Save.bestStars.TryGetValue(levelIndex, out var s) ? s : 0;

        /// <summary>甜品图鉴：首次拾取记入存档。</summary>
        public void CollectItem(string itemId)
        {
            if (string.IsNullOrEmpty(itemId) || Save.collectedItems.Contains(itemId)) return;
            Save.collectedItems.Add(itemId);
            WriteSave();
            GameEvents.Toast($"✦ 新甜品入图鉴！({Save.collectedItems.Count})");
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
            // 字典 → 平铺列表（JsonUtility 只序列化字段，不序列化字典）
            Save.bestTimeKeys.Clear();
            Save.bestTimeValues.Clear();
            foreach (var kv in Save.bestTime) { Save.bestTimeKeys.Add(kv.Key); Save.bestTimeValues.Add(kv.Value); }
            Save.bestStarKeys.Clear();
            Save.bestStarValues.Clear();
            foreach (var kv in Save.bestStars) { Save.bestStarKeys.Add(kv.Key); Save.bestStarValues.Add(kv.Value); }
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
        public System.Collections.Generic.List<int> bestStarKeys = new System.Collections.Generic.List<int>();
        public System.Collections.Generic.List<int> bestStarValues = new System.Collections.Generic.List<int>();
        public System.Collections.Generic.List<string> collectedItems = new System.Collections.Generic.List<string>();

        [System.NonSerialized]
        public System.Collections.Generic.Dictionary<int, float> bestTime = new System.Collections.Generic.Dictionary<int, float>();

        [System.NonSerialized]
        public System.Collections.Generic.Dictionary<int, int> bestStars = new System.Collections.Generic.Dictionary<int, int>();

        public void Ensure()
        {
            if (unlockedSkills == null) unlockedSkills = new System.Collections.Generic.List<string>();
            if (bestTimeKeys == null) bestTimeKeys = new System.Collections.Generic.List<int>();
            if (bestTimeValues == null) bestTimeValues = new System.Collections.Generic.List<float>();
            if (bestTime == null) bestTime = new System.Collections.Generic.Dictionary<int, float>();
            if (bestStars == null) bestStars = new System.Collections.Generic.Dictionary<int, int>();
            if (collectedItems == null) collectedItems = new System.Collections.Generic.List<string>();

            // 反序列化后把平铺列表还原为字典
            if (bestTime.Count == 0 && bestTimeKeys.Count == bestTimeValues.Count)
                for (int i = 0; i < bestTimeKeys.Count; i++) bestTime[bestTimeKeys[i]] = bestTimeValues[i];
            if (bestStars.Count == 0 && bestStarKeys.Count == bestStarValues.Count)
                for (int i = 0; i < bestStarKeys.Count; i++) bestStars[bestStarKeys[i]] = bestStarValues[i];
        }
    }
}
