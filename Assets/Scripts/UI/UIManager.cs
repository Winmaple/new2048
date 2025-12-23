using UnityEngine;
using TMPro;

namespace Game2048Upgrade
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance;

        [SerializeField] private TextMeshProUGUI scoreText;

        void Awake()
        {
            Instance = this;
        }

        public void UpdateScore(int score)
        {
            scoreText.text = "Score: " + score;
        }
    }
}
