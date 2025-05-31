using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonMaster
{
    public class GrenadeProjectile : MonoBehaviour
    {
        public static event EventHandler OnAnyGrenadeExploded;
        private Vector3 targetPosition;
        private Action onGrenadeBehaviourComplete;

        private void Update()
        {
            Vector3 moveDir = (targetPosition - transform.position).normalized;

            float moveSpeed = 15f;
            transform.position += moveDir * moveSpeed * Time.deltaTime;

            float reachedTargetDistance = .2f;
            if (Vector3.Distance(transform.position, targetPosition) < reachedTargetDistance)
            {
                float damageRadius = 2f;
                Collider2D[] colliderArray = Physics2D.OverlapCircleAll(targetPosition, damageRadius);

                foreach (Collider2D collider in colliderArray)
                {
                    if (collider.TryGetComponent<Unit>(out Unit targetUnit))
                    {
                        targetUnit.Damage(30);
                    }
                    if (collider.TryGetComponent<Destructible>(out Destructible destructible))
                    {
                        destructible.Damage();
                    }
                }

                OnAnyGrenadeExploded?.Invoke(this, EventArgs.Empty);
                Destroy(gameObject);
                onGrenadeBehaviourComplete();
            }
        }

        public void Setup(GridPosition targetGridPosition, Action onGrenadeBehaviourComplete)
        {
            this.onGrenadeBehaviourComplete = onGrenadeBehaviourComplete;
            targetPosition = LevelGrid.Instance.GetWorldPosition(targetGridPosition);
        }
    }
}
