using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArcherCraft.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Game State")]
        [SerializeField] private bool isGameOver;
        [SerializeField] private int parriedBulletsCount;
        [SerializeField] private int enemiesDefeatedCount;

        public event Action<int> OnParryCountChanged;
        public event Action<int> OnEnemyKilled;
        public event Action OnPlayerDied;

        public int ParriedBulletsCount => parriedBulletsCount;
        public int EnemiesDefeatedCount => enemiesDefeatedCount;
        public bool IsGameOver => isGameOver;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            // R키로 씬 재시작
            if (Input.GetKeyDown(KeyCode.R))
            {
                RestartCurrentScene();
            }
        }

        public void AddParryCount()
        {
            parriedBulletsCount++;
            OnParryCountChanged?.Invoke(parriedBulletsCount);
        }

        public void AddEnemyKill()
        {
            enemiesDefeatedCount++;
            OnEnemyKilled?.Invoke(enemiesDefeatedCount);
        }

        public void TriggerGameOver()
        {
            isGameOver = true;
            OnPlayerDied?.Invoke();
        }

        public void RestartCurrentScene()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
