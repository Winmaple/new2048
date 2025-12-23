using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace Game2048Upgrade
{
    public class Tile : MonoBehaviour
    {
        public int Value { get; private set; }
        public int X { get; private set; }
        public int Y { get; private set; }

        [SerializeField] private TextMeshProUGUI valueText;
        [SerializeField] private Image background;
        [SerializeField] private Image iconImage; // 新增：图案显示
        [SerializeField] private Image highlightOverlay; // �������ֲ�

        private Color normalColor;
        private bool isHighlighted = false;

        public void Init(int value, int x, int y)
        {
            Value = value;
            X = x;
            Y = y;
            UpdateUI();
        }

        public void UpdatePosition(int x, int y)
        {
            X = x;
            Y = y;
        }

        public void UpdateValue(int newValue)
        {
            Value = newValue;
            UpdateUI();
        }

        public void SetHighlight(bool highlight)
        {
            // �������Ƿ��ѱ�����
            if (this == null || background == null)
                return;

            isHighlighted = highlight;
            if (highlightOverlay != null)
            {
                highlightOverlay.enabled = highlight;
            }
            else
            {
                // ���û�е����ĸ����㣬ʹ����ɫ����
                if (highlight)
                {
                    background.color = Color.Lerp(normalColor, Color.white, 0.3f);
                }
                else
                {
                    background.color = normalColor;
                }
            }
        }

        private void UpdateUI()
        {
            if (valueText == null)
            {
                Debug.LogError("ValueText is null! Please assign TextMeshProUGUI in Tile prefab.", this);
                return;
            }

            if (background == null)
            {
                Debug.LogError("Background is null! Please assign Image component in Tile prefab.", this);
                return;
            }

            valueText.text = Value.ToString();
            // ��̬���������С
            int length = valueText.text.Length;
            if (length <= 2) valueText.fontSize = 50;
            else if (length == 3) valueText.fontSize = 40;
            else valueText.fontSize = 30;

            // ������ֵ�ı���ɫ����ʵ�֣�
            normalColor = GetColorByValue(Value);
            background.color = normalColor;

            // 图标支持：从配置中获取对应精灵，若存在则显示图标并隐藏文本
            Sprite icon = null;
            if (GridManager.Instance != null && GridManager.Instance.Config != null)
            {
                icon = GridManager.Instance.Config.GetIconForValue(Value);
            }

            if (iconImage != null)
            {
                if (icon != null)
                {
                    iconImage.sprite = icon;
                    iconImage.enabled = true;
                    if (valueText != null) valueText.enabled = false;
                }
                else
                {
                    iconImage.enabled = false;
                    if (valueText != null) valueText.enabled = true;
                }
            }
        }

        private Color GetColorByValue(int value)
        {
            switch (value)
            {
                case 2: return new Color(0.93f, 0.89f, 0.85f);
                case 4: return new Color(0.93f, 0.88f, 0.78f);
                case 8: return new Color(0.95f, 0.69f, 0.47f);
                case 16: return new Color(0.96f, 0.58f, 0.39f);
                case 32: return new Color(0.96f, 0.48f, 0.37f);
                case 64: return new Color(0.96f, 0.37f, 0.23f);
                default: return new Color(0.93f, 0.76f, 0.18f);
            }
        }
    }
}
