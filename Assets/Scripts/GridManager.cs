using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine.EventSystems;

namespace Game2048Upgrade
{
    public class GridManager : MonoBehaviour
    {
        public static GridManager Instance;

        [SerializeField] private GameConfig config;
        [SerializeField] private RectTransform gridParent;
        [SerializeField] private GridLayoutGroup gridLayout;

        private Tile[,] grid;
        private List<Tile> currentSelection = new List<Tile>();
        private bool isProcessing = false;

        private int currentScore = 0;
        private int initialValue = 0; // 记录起始点的数字

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            Debug.Log("GridManager Start() called");
            InitializeGrid();
        }

        private void InitializeGrid()
        {
            // 检查配置
            if (config == null)
            {
                Debug.LogError("GameConfig is null! Please assign it in the Inspector.");
                return;
            }

            if (gridParent == null)
            {
                Debug.LogError("GridParent is null! Please assign it in the Inspector.");
                return;
            }

            if (gridLayout == null)
            {
                Debug.LogError("GridLayout is null! Please assign it in the Inspector.");
                return;
            }

            if (config.tilePrefab == null)
            {
                Debug.LogError("Tile Prefab is null! Please assign it in GameConfig.");
                return;
            }

            Debug.Log($"Initializing grid: {config.columns}x{config.rows}");
            Debug.Log($"Config Cell Size: {config.cellSize}");

            grid = new Tile[config.columns, config.rows];

            // 设置布局自适应
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = config.columns;
            gridLayout.cellSize = config.cellSize;
            gridLayout.spacing = Vector2.zero; // 确保间距为0，格子紧密相邻
            gridLayout.padding = new RectOffset(0, 0, 0, 0); // 确保没有内边距

            Debug.Log($"Grid Layout Cell Size set to: {gridLayout.cellSize}");

            // 生成初始棋盘（所有格子随机生成2, 4, 8, 16）
            for (int y = 0; y < config.rows; y++)
            {
                for (int x = 0; x < config.columns; x++)
                {
                    CreateTile(x, y, true);
                }
            }

            Debug.Log($"Grid initialized with {config.columns * config.rows} tiles");
        }

        private void CreateTile(int x, int y, bool isInitialSpawn = false)
        {
            CreateTileAndReturn(x, y, isInitialSpawn);
        }

        private Tile CreateTileAndReturn(int x, int y, bool isInitialSpawn = false)
        {
            GameObject go = Instantiate(config.tilePrefab, gridParent);
            Tile tile = go.GetComponent<Tile>();

            if (tile == null)
            {
                Debug.LogError($"Tile prefab does not have Tile component at ({x}, {y})!");
                return null;
            }

            // 初始和补位都随机生成2, 4, 8, 16
            int[] possibleValues = { 2, 4, 8, 16 };
            int randomValue = possibleValues[Random.Range(0, possibleValues.Length)];

            tile.Init(randomValue, x, y);
            grid[x, y] = tile;

            // 设置正确的 UI 层级位置
            int siblingIndex = y * config.columns + x;
            tile.transform.SetSiblingIndex(siblingIndex);

            // 非初始生成时播放下落动画
            if (!isInitialSpawn)
            {
                StartCoroutine(PlayDropAnimation(tile));
            }

            Debug.Log($"Created tile at ({x}, {y}) with value {randomValue}");
            return tile;
        }

        private IEnumerator PlayDropAnimation(Tile tile)
        {
            if (tile == null) yield break;

            RectTransform rectTransform = tile.GetComponent<RectTransform>();
            if (rectTransform == null) yield break;

            // 保存目标缩放
            Vector3 targetScale = Vector3.one;

            // 从缩放0开始
            rectTransform.localScale = Vector3.zero;

            // 动画时长
            float duration = 0.2f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                // 检查对象是否还存在
                if (tile == null || rectTransform == null) yield break;

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // 使用平滑缓动，不超过1.0，避免格子变大
                float scale = Mathf.SmoothStep(0f, 1f, t);
                rectTransform.localScale = targetScale * scale;
                yield return null;
            }

            // 确保最终缩放正确
            if (rectTransform != null)
            {
                rectTransform.localScale = targetScale;
            }
        }

        void Update()
        {
            if (isProcessing) return;

            HandleInput();
        }

        private void HandleInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                StartSelection();
            }
            else if (Input.GetMouseButton(0))
            {
                UpdateSelection();
            }
            else if (Input.GetMouseButtonUp(0))
            {
                FinishSelection();
            }
        }

        private void StartSelection()
        {
            Tile tile = GetTileUnderMouse();
            if (tile != null)
            {
                ClearHighlights(); // 清除之前的高亮
                currentSelection.Clear();
                currentSelection.Add(tile);
                initialValue = tile.Value; // 记录起始数字
                UpdateHighlights();
            }
        }

        private void UpdateSelection()
        {
            Tile tile = GetTileUnderMouse();

            if (tile == null) return;

            // 检查列表是否为空
            if (currentSelection.Count == 0) return;

            // 如果已经在选择列表中，不处理
            if (currentSelection.Contains(tile)) return;

            Tile lastTile = currentSelection[currentSelection.Count - 1];

            // 检查是否相邻（不允许斜线连接）且数字与起始点相同
            if (IsAdjacentNoDiagonal(lastTile, tile))
            {
                if (tile.Value == initialValue)
                {
                    // 数字相同，添加到选择列表
                    currentSelection.Add(tile);
                    UpdateHighlights();
                }
                else
                {
                    // 数字不同，取消所有高亮并重置选择
                    ClearHighlights();
                    currentSelection.Clear();
                }
            }
        }

        private void FinishSelection()
        {
            if (currentSelection.Count >= 2)
            {
                StartCoroutine(ProcessMerge());
            }
            else
            {
                ClearHighlights();
                currentSelection.Clear();
            }
        }

        private void UpdateHighlights()
        {
            // 只有选中格子数 >= 2 时才高亮
            if (currentSelection.Count >= 2)
            {
                foreach (var tile in currentSelection)
                {
                    // 检查 tile 是否还存在
                    if (tile != null)
                    {
                        tile.SetHighlight(true);
                    }
                }
            }
            else
            {
                ClearHighlights();
            }
        }

        private void ClearHighlights()
        {
            foreach (var tile in currentSelection)
            {
                // 检查 tile 是否还存在（可能已被销毁）
                if (tile != null)
                {
                    tile.SetHighlight(false);
                }
            }
        }

        private IEnumerator ProcessMerge()
        {
            isProcessing = true;

            // 保留最后一个格子作为合并目标
            Tile targetTile = currentSelection[currentSelection.Count - 1];
            int targetX = targetTile.X;
            int targetY = targetTile.Y;

            Debug.Log($"合并到位置: ({targetX}, {targetY}), 原值: {targetTile.Value}");

            // 按照2048规则：多个相同数字合并生成下一个目标数字
            int newValue = targetTile.Value * 2;

            // 计算得分
            int score = config.GetScore(targetTile.Value, currentSelection.Count);
            AddScore(score);

            // 销毁除最后一个外的所有格子（包括第一个到倒数第二个）
            for (int i = 0; i < currentSelection.Count - 1; i++)
            {
                Tile t = currentSelection[i];
                Debug.Log($"销毁格子: ({t.X}, {t.Y})");

                // 清除网格引用
                grid[t.X, t.Y] = null;

                // 销毁游戏对象
                Destroy(t.gameObject);
            }

            // 等待一帧确保销毁完成
            yield return null;

            // 更新最后一个格子的值
            targetTile.UpdateValue(newValue);

            // 确保目标格子的网格位置正确
            grid[targetX, targetY] = targetTile;
            targetTile.UpdatePosition(targetX, targetY);

            Debug.Log($"合并完成: 位置({targetX}, {targetY}), 新值: {newValue}");

            yield return new WaitForSeconds(0.2f);

            yield return StartCoroutine(HandleGravity());

            ClearHighlights();
            currentSelection.Clear();
            isProcessing = false;
        }

        private IEnumerator HandleGravity()
        {
            // 1. 垂直下落：每一列中，上方的格子向下填补空位
            bool hasVerticalMove;
            do
            {
                hasVerticalMove = false;
                for (int x = 0; x < config.columns; x++)
                {
                    // 从下往上检查每个位置
                    for (int y = config.rows - 1; y >= 0; y--)
                    {
                        if (grid[x, y] == null)
                        {
                            // 向上找第一个不为空的格子
                            for (int k = y - 1; k >= 0; k--)
                            {
                                if (grid[x, k] != null)
                                {
                                    // 将上方格子移动到当前空位
                                    grid[x, y] = grid[x, k];
                                    grid[x, k] = null;
                                    grid[x, y].UpdatePosition(x, y);
                                    hasVerticalMove = true;
                                    break;
                                }
                            }
                        }
                    }
                }

                if (hasVerticalMove)
                {
                    yield return new WaitForSeconds(0.15f); // 等待垂直下落动画
                }
            } while (hasVerticalMove);

            // 2. 补位逻辑：统计每列的空位数量，从顶部生成相应数量的新格子
            List<Tile> newTiles = new List<Tile>();
            for (int x = 0; x < config.columns; x++)
            {
                // 统计这一列有多少个空位
                int emptyCount = 0;
                for (int y = 0; y < config.rows; y++)
                {
                    if (grid[x, y] == null)
                    {
                        emptyCount++;
                    }
                }

                // 从顶部生成相应数量的新格子
                for (int i = 0; i < emptyCount; i++)
                {
                    Tile newTile = CreateTileAndReturn(x, i, false);
                    if (newTile != null)
                    {
                        newTiles.Add(newTile);
                    }
                }
            }

            // 等待所有新格子的生成动画完成
            if (newTiles.Count > 0)
            {
                yield return StartCoroutine(WaitForAllDropAnimations(newTiles));
            }

            // 3. 再次执行下落，让新生成的格子掉落到底部
            yield return StartCoroutine(HandleGravityAfterSpawn());

            // 4. 最后统一刷新所有格子的UI层级顺序
            RefreshAllTileSiblingIndices();
        }

        // 等待所有格子的下落动画完成
        private IEnumerator WaitForAllDropAnimations(List<Tile> tiles)
        {
            float duration = 0.3f; // 增加等待时间，确保动画完全完成
            yield return new WaitForSeconds(duration);
        }

        // 刷新所有格子的UI层级顺序（在所有移动和生成完成后调用）
        private void RefreshAllTileSiblingIndices()
        {
            for (int y = 0; y < config.rows; y++)
            {
                for (int x = 0; x < config.columns; x++)
                {
                    if (grid[x, y] != null)
                    {
                        int siblingIndex = y * config.columns + x;
                        grid[x, y].transform.SetSiblingIndex(siblingIndex);
                    }
                }
            }
        }

        // 新格子生成后的下落处理
        private IEnumerator HandleGravityAfterSpawn()
        {
            bool hasMoved;
            do
            {
                hasMoved = false;
                for (int x = 0; x < config.columns; x++)
                {
                    for (int y = config.rows - 1; y >= 0; y--)
                    {
                        if (grid[x, y] == null)
                        {
                            for (int k = y - 1; k >= 0; k--)
                            {
                                if (grid[x, k] != null)
                                {
                                    grid[x, y] = grid[x, k];
                                    grid[x, k] = null;
                                    grid[x, y].UpdatePosition(x, y);
                                    hasMoved = true;
                                    break;
                                }
                            }
                        }
                    }
                }

                if (hasMoved)
                {
                    yield return new WaitForSeconds(0.15f);
                }
            } while (hasMoved);
        }

        // 检查是否相邻（上下左右，不包括斜线）
        private bool IsAdjacentNoDiagonal(Tile a, Tile b)
        {
            int dx = Mathf.Abs(a.X - b.X);
            int dy = Mathf.Abs(a.Y - b.Y);
            // 必须只在一个方向上相差1，另一个方向相同
            return (dx == 1 && dy == 0) || (dx == 0 && dy == 1);
        }

        private Tile GetTileUnderMouse()
        {
            // 简单实现：通过射线检测或UI点击
            PointerEventData eventData = new PointerEventData(EventSystem.current);
            eventData.position = Input.mousePosition;
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);
            foreach (var result in results)
            {
                Tile t = result.gameObject.GetComponent<Tile>();
                if (t != null) return t;
            }
            return null;
        }

        private void AddScore(int score)
        {
            currentScore += score;
            UIManager.Instance.UpdateScore(currentScore);
        }
    }
}
