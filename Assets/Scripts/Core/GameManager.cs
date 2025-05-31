using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace DungeonMaster
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private Unit unit;

        private void Start()
        {
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.T))
            {
                Debug.Log($"Pressed T");
            }
        }
    }
}