using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonMaster
{
    public class EnemyAI : MonoBehaviour
    {
        [SerializeField] private AIState state;
        private float timer;

        private void Awake()
        {
            state = AIState.WaitingForEnemyTurn;
        }

        private void Start()
        {
            TurnSystem.Instance.OnTurnChanged += TurnSystem_OnTurnChanged;
        }

        private void Update()
        {
            if (TurnSystem.Instance.IsPlayerTurn())
            {
                return;
            }

            switch (state)
            {
                case AIState.WaitingForEnemyTurn:
                    break;
                case AIState.TakingTurn:
                    timer -= Time.deltaTime;
                    if (timer <= 0f)
                    {
                        Debug.Log($"[EnemyAI] AIState.TakingTurn");
                        if (TryTakeEnemyAIAction(SetStateTakingTurn))
                        {
                            state = AIState.Busy;
                            Debug.Log($"[EnemyAI] Enemy is busy");
                        }
                        else
                        {
                            // No more enemies have actions they can take, end enemy turn
                            TurnSystem.Instance.NextTurn();
                        }
                    }
                    break;
                case AIState.Busy:
                    break;
            }

        }

        private void SetStateTakingTurn()
        {
            timer = 0.5f;
            state = AIState.TakingTurn;
        }

        private void TurnSystem_OnTurnChanged(object sender, EventArgs e)
        {
            if (!TurnSystem.Instance.IsPlayerTurn())
            {
                state = AIState.TakingTurn;
                timer = 2f;
            }
        }

        private bool TryTakeEnemyAIAction(Action onEnemyAIActionComplete)
        {
            Debug.Log($"[EnemyAI] Enemy Try Take Enemy AIAction. Enemy list {UnitManager.Instance.GetEnemyUnitList()}");
            foreach (Unit enemyUnit in UnitManager.Instance.GetEnemyUnitList())
            {
                Debug.Log($"[EnemyAI] Enemies: {enemyUnit}");
                if (TryTakeEnemyAIAction(enemyUnit, onEnemyAIActionComplete))
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryTakeEnemyAIAction(Unit enemyUnit, Action onEnemyAIActionComplete)
        {
            Debug.Log($"[EnemyAI] Enemy will atack: {enemyUnit}");

            EnemyAIAction bestEnemyAIAction = null;
            BaseAction bestBaseAction = null;

            foreach (BaseAction baseAction in enemyUnit.GetBaseActionArray())
            {
                if (!enemyUnit.CanSpendActionPointsToTakeAction(baseAction))
                {
                    // Enemy cannot afford this action
                    continue;
                }

                if (bestEnemyAIAction == null)
                {
                    bestEnemyAIAction = baseAction.GetBestEnemyAIAction();
                    bestBaseAction = baseAction;
                }
                else
                {
                    EnemyAIAction testEnemyAIAction = baseAction.GetBestEnemyAIAction();
                    if (testEnemyAIAction != null && testEnemyAIAction.actionValue > bestEnemyAIAction.actionValue)
                    {
                        bestEnemyAIAction = testEnemyAIAction;
                        bestBaseAction = baseAction;
                    }
                }

            }

            if (bestEnemyAIAction != null && enemyUnit.TrySpendActionPointsToTakeAction(bestBaseAction))
            {
                bestBaseAction.TakeAction(bestEnemyAIAction.gridPosition, onEnemyAIActionComplete);
                return true;
            }
            else
            {
                return false;
            }
        }


    }
}
