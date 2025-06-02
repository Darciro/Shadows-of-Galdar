using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonMaster
{
    public class Chest : MonoBehaviour, IInteractable
    {
        [SerializeField] private bool isOpen;
        private GridPosition gridPosition;
        // private Animator animator;
        private Action onInteractionComplete;
        private bool isActive;
        private float timer;
        private SpriteRenderer chestSpriteRenderer;
        [SerializeField] private Sprite openChestSpritePrefab;
        [SerializeField] private Sprite closeChestSpritePrefab;


        private void Awake()
        {
            // animator = GetComponent<Animator>();
        }

        private void Start()
        {
            gridPosition = LevelGrid.Instance.GetGridPosition(transform.position);
            LevelGrid.Instance.SetInteractableAtGridPosition(gridPosition, this);
            chestSpriteRenderer = GetComponent<SpriteRenderer>();

            if (isOpen)
            {
                OpenDoor();
            }
            else
            {
                CloseDoor();
            }
        }

        private void Update()
        {
            if (!isActive)
            {
                return;
            }

            timer -= Time.deltaTime;

            if (timer <= 0f)
            {
                isActive = false;
                onInteractionComplete();
            }
        }

        public void Interact(Action onInteractionComplete)
        {
            this.onInteractionComplete = onInteractionComplete;
            isActive = true;
            timer = .5f;

            if (isOpen)
            {
                CloseDoor();
            }
            else
            {
                OpenDoor();
            }
        }

        private void OpenDoor()
        {
            Debug.Log($"[Chest] chest in position {gridPosition} was opened");
            isOpen = true;
            // animator.SetBool("IsOpen", isOpen);
            chestSpriteRenderer.sprite = openChestSpritePrefab;
            // Pathfinding.Instance.SetIsWalkableGridPosition(gridPosition, true);
        }

        private void CloseDoor()
        {
            Debug.Log($"[Chest] chest in position {gridPosition} was closed");
            isOpen = false;
            // animator.SetBool("IsOpen", isOpen);
            chestSpriteRenderer.sprite = closeChestSpritePrefab;
            // Pathfinding.Instance.SetIsWalkableGridPosition(gridPosition, false);
        }
    }
}
