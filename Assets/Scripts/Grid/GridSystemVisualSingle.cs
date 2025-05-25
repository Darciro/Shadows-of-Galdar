using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonMaster
{
    public class GridSystemVisualSingle : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;

        void Start()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        // [SerializeField] private MeshRenderer meshRenderer;
        public void Show(GridVisualType material)
        {
            Color color;
            switch (material)
            {
                case GridVisualType.Blue:
                    // spriteRenderer.color = new Color(1f, 1f, 1f, 0.2f);
                    ColorUtility.TryParseHtmlString("#0029FF80", out color);
                    break;

                case GridVisualType.Red:
                    ColorUtility.TryParseHtmlString("#FF002080", out color);
                    break;

                case GridVisualType.RedSoft:
                    ColorUtility.TryParseHtmlString("#FF909E80", out color);
                    break;

                case GridVisualType.Yellow:
                    ColorUtility.TryParseHtmlString("#FFDD8F80", out color);
                    break;

                default:
                    ColorUtility.TryParseHtmlString("#FFFFFF", out color);
                    break;
            }

            spriteRenderer.color = color;
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            // meshRenderer.enabled = false;
            gameObject.SetActive(false);
        }
    }
}