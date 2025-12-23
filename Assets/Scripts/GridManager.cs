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
        private int initialValue = 0; // ��¼��ʼ�������

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
            // �������
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

            // ���ò�������Ӧ
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = config.columns;
            gridLayout.cellSize = config.cellSize;
            gridLayout.spacing = Vector2.zero; // ȷ�����Ϊ0�����ӽ�������
            gridLayout.padding = new RectOffset(0, 0, 0, 0); // ȷ��û���ڱ߾�

            Debug.Log($"Grid Layout Cell Size set to: {gridLayout.cellSize}");

            // ���ɳ�ʼ���̣����и����������2, 4, 8, 16��
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

            // ��ʼ�Ͳ�λ���������2, 4, 8, 16
            int[] possibleValues = { 2, 4, 8, 16 };
            int randomValue = possibleValues[Random.Range(0, possibleValues.Length)];

            tile.Init(randomValue, x, y);
            grid[x, y] = tile;

            // 确保初始创建的格子缩放为动画结束时的大小，避免与播放动画的格子尺寸不一致
            RectTransform tileRect = tile.GetComponent<RectTransform>();
            if (tileRect != null)
            {
                tileRect.localScale = Vector3.one;
            }

            // ������ȷ�� UI �㼶λ��
            int siblingIndex = y * config.columns + x;
            tile.transform.SetSiblingIndex(siblingIndex);

            // �ǳ�ʼ����ʱ�������䶯��
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
            // 确保布局已计算，获取目标位置
            if (gridParent != null)
            {
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(gridParent);
            }

            Vector2 targetPos = rectTransform.anchoredPosition;

            // 计算从上方开始的起始位置（基于格子高度和行数）
            float dropOffset = 0f;
            if (config != null)
            {
                dropOffset = config.cellSize.y * config.rows + 50f;
            }
            else if (gridParent != null)
            {
                dropOffset = gridParent.rect.height + 50f;
            }

            Vector2 startPos = targetPos + new Vector2(0f, dropOffset);

            // 保证缩放为正常大小
            rectTransform.localScale = Vector3.one;

            // 设置起始位置并做下落插值
            rectTransform.anchoredPosition = startPos;

            float duration = 0.25f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (tile == null || rectTransform == null) yield break;
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                rectTransform.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
                yield return null;
            }

            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = targetPos;
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
                ClearHighlights(); // ���֮ǰ�ĸ���
                currentSelection.Clear();
                currentSelection.Add(tile);
                initialValue = tile.Value; // ��¼��ʼ����
                UpdateHighlights();
            }
        }

        private void UpdateSelection()
        {
            Tile tile = GetTileUnderMouse();

            if (tile == null) return;

            // ����б��Ƿ�Ϊ��
            if (currentSelection.Count == 0) return;

            // ����Ѿ���ѡ���б��У�������
            if (currentSelection.Contains(tile)) return;

            Tile lastTile = currentSelection[currentSelection.Count - 1];

            // ����Ƿ����ڣ�������б�����ӣ�����������ʼ����ͬ
            if (IsAdjacentNoDiagonal(lastTile, tile))
            {
                if (tile.Value == initialValue)
                {
                    // ������ͬ�����ӵ�ѡ���б�
                    currentSelection.Add(tile);
                    UpdateHighlights();
                }
                else
                {
                    // ���ֲ�ͬ��ȡ�����и���������ѡ��
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
            // ֻ��ѡ�и����� >= 2 ʱ�Ÿ���
            if (currentSelection.Count >= 2)
            {
                foreach (var tile in currentSelection)
                {
                    // ��� tile �Ƿ񻹴���
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
                // ��� tile �Ƿ񻹴��ڣ������ѱ����٣�
                if (tile != null)
                {
                    tile.SetHighlight(false);
                }
            }
        }

        private IEnumerator ProcessMerge()
        {
            isProcessing = true;

            // �������һ��������Ϊ�ϲ�Ŀ��
            Tile targetTile = currentSelection[currentSelection.Count - 1];
            int targetX = targetTile.X;
            int targetY = targetTile.Y;

            Debug.Log($"�ϲ���λ��: ({targetX}, {targetY}), ԭֵ: {targetTile.Value}");

            // 在销毁其他格子前禁用布局，避免中间重排导致目标格子闪烁/消失
            if (gridLayout != null)
            {
                gridLayout.enabled = false;
            }

            // ����2048���򣺶����ͬ���ֺϲ�������һ��Ŀ������
            int newValue = targetTile.Value * 2;

            // ����÷�
            int score = config.GetScore(targetTile.Value, currentSelection.Count);
            AddScore(score);

            // ���ٳ����һ��������и��ӣ�������һ���������ڶ�����
            for (int i = 0; i < currentSelection.Count - 1; i++)
            {
                Tile t = currentSelection[i];
                Debug.Log($"���ٸ���: ({t.X}, {t.Y})");

                // �����������
                grid[t.X, t.Y] = null;

                // ������Ϸ����
                Destroy(t.gameObject);
            }

            // �ȴ�һ֡ȷ���������
            yield return null;

            // �������һ�����ӵ�ֵ
            targetTile.UpdateValue(newValue);

            // 确保 UI 状态更新后再继续（布局仍禁用）
            if (gridParent != null)
            {
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(gridParent);
            }

            // ȷ��Ŀ����ӵ�����λ����ȷ
            grid[targetX, targetY] = targetTile;
            targetTile.UpdatePosition(targetX, targetY);

            Debug.Log($"�ϲ����: λ��({targetX}, {targetY}), ��ֵ: {newValue}");

            yield return new WaitForSeconds(0.2f);

            yield return StartCoroutine(HandleGravity());

            ClearHighlights();
            currentSelection.Clear();
            isProcessing = false;
        }

        private IEnumerator HandleGravity()
        {
            // 在进行大量格子移动和销毁/创建时，临时禁用布局重排
            if (gridLayout != null)
            {
                gridLayout.enabled = false;
            }

            
            bool hasVerticalMove;
            do
            {
                hasVerticalMove = false;
                for (int x = 0; x < config.columns; x++)
                {
                   
                    for (int y = config.rows - 1; y >= 0; y--)
                    {
                        if (grid[x, y] == null)
                        {
                            // �����ҵ�һ����Ϊ�յĸ���
                            for (int k = y - 1; k >= 0; k--)
                            {
                                if (grid[x, k] != null)
                                {
                                    // ���Ϸ������ƶ�����ǰ��λ
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
                    yield return new WaitForSeconds(0.15f); // �ȴ���ֱ���䶯��
                }
            } while (hasVerticalMove);

            // 2. ��λ�߼���ͳ��ÿ�еĿ�λ�������Ӷ���������Ӧ�������¸���
            List<Tile> newTiles = new List<Tile>();
            for (int x = 0; x < config.columns; x++)
            {
                // ͳ����һ���ж��ٸ���λ
                int emptyCount = 0;
                for (int y = 0; y < config.rows; y++)
                {
                    if (grid[x, y] == null)
                    {
                        emptyCount++;
                    }
                }

                // �Ӷ���������Ӧ�������¸���
                for (int i = 0; i < emptyCount; i++)
                {
                    Tile newTile = CreateTileAndReturn(x, i, false);
                    if (newTile != null)
                    {
                        newTiles.Add(newTile);
                    }
                }
            }

            // �ȴ������¸��ӵ����ɶ������
            if (newTiles.Count > 0)
            {
                yield return StartCoroutine(WaitForAllDropAnimations(newTiles));
            }

            // 3. �ٴ�ִ�����䣬�������ɵĸ��ӵ��䵽�ײ�
            yield return StartCoroutine(HandleGravityAfterSpawn());

            // 4. ���ͳһˢ�����и��ӵ�UI�㼶˳��
            RefreshAllTileSiblingIndices();

            // 恢复布局并强制重建一次，确保最终位置正确且只发生一次重排
            if (gridLayout != null && gridParent != null)
            {
                gridLayout.enabled = true;
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(gridParent);
            }
        }

        // �ȴ����и��ӵ����䶯�����
        private IEnumerator WaitForAllDropAnimations(List<Tile> tiles)
        {
            float duration = 0.3f; // ���ӵȴ�ʱ�䣬ȷ��������ȫ���
            yield return new WaitForSeconds(duration);
        }

        // ˢ�����и��ӵ�UI�㼶˳���������ƶ���������ɺ���ã�
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

        // �¸������ɺ�����䴦��
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

        // ����Ƿ����ڣ��������ң�������б�ߣ�
        private bool IsAdjacentNoDiagonal(Tile a, Tile b)
        {
            int dx = Mathf.Abs(a.X - b.X);
            int dy = Mathf.Abs(a.Y - b.Y);
            // ����ֻ��һ�����������1����һ��������ͬ
            return (dx == 1 && dy == 0) || (dx == 0 && dy == 1);
        }

        private Tile GetTileUnderMouse()
        {
            // ��ʵ�֣�ͨ�����߼���UI���
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
