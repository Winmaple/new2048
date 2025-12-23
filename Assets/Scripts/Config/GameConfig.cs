using UnityEngine;
using System.Collections.Generic;

namespace Game2048Upgrade
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "2048Upgrade/GameConfig")]
    public class GameConfig : ScriptableObject
    {
        [Header("Grid Settings")]
        public int rows = 8;
        public int columns = 8;
        public float spacing = 0f;  // 格子紧密排列，无间隙
        public Vector2 cellSize = new Vector2(70, 70);

        [Header("Score Settings")]
        public List<ScoreMapping> scoreMappings;

        [Header("Prefab References")]
        public GameObject tilePrefab;

        [Header("Value Icons")]
        // Inspector 中按 2,4,8... 顺序填入对应图标
        public Sprite[] valueIcons;

        public int GetScore(int value, int count)
        {
            foreach (var mapping in scoreMappings)
            {
                if (mapping.value == value)
                {
                    return mapping.baseScore * count;
                }
            }
            return value * count;
        }

        public Sprite GetIconForValue(int value)
        {
            if (valueIcons == null || valueIcons.Length == 0) return null;
            int index = 0;
            int v = 2;
            while (v < value && index < valueIcons.Length - 1)
            {
                v <<= 1;
                index++;
            }
            if (v == value && index < valueIcons.Length)
            {
                return valueIcons[index];
            }
            return null;
        }
    }

    [System.Serializable]
    public class ScoreMapping
    {
        public int value;
        public int baseScore;
    }
}
