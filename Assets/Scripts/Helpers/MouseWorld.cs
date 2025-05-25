using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DungeonMaster
{
    public class MouseWorld : MonoBehaviour
    {
        private static MouseWorld instance;
        [SerializeField] private LayerMask mousePlaneLayerMask;

        void Awake()
        {
            instance = this;
        }

        private void Update()
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            transform.position = mousePos;
        }

        public static Vector2 GetPosition()
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero, float.MaxValue, instance.mousePlaneLayerMask);
            return hit.point;
        }

    }
}
