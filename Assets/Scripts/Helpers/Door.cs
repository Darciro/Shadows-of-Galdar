using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonMaster
{
    public class Door : MonoBehaviour, IInteractable
    {
        [SerializeField] private bool isOpen;
        private GridPosition gridPosition;
        // private Animator animator;
        private Action onInteractionComplete;
        private bool isActive;
        private float timer;
        private SpriteRenderer doorSpriteRenderer;
        [SerializeField] private Sprite openDoorSpritePrefab;
        [SerializeField] private Sprite closeDoorSpritePrefab;


        private void Awake()
        {
            // animator = GetComponent<Animator>();
        }

        private void Start()
        {
            gridPosition = LevelGrid.Instance.GetGridPosition(transform.position);
            LevelGrid.Instance.SetInteractableAtGridPosition(gridPosition, this);
            doorSpriteRenderer = GetComponent<SpriteRenderer>();

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
            Debug.Log($"[Door] door in position {gridPosition} was opened");
            isOpen = true;
            // animator.SetBool("IsOpen", isOpen);
            doorSpriteRenderer.sprite = openDoorSpritePrefab;
            Pathfinding.Instance.SetIsWalkableGridPosition(gridPosition, true);
        }

        private void CloseDoor()
        {
            Debug.Log($"[Door] door in position {gridPosition} was closed");
            isOpen = false;
            // animator.SetBool("IsOpen", isOpen);
            doorSpriteRenderer.sprite = closeDoorSpritePrefab;
            Pathfinding.Instance.SetIsWalkableGridPosition(gridPosition, false);
        }
    }
}
