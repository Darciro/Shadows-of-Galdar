using System.Collections;
using UnityEngine;

public class SceneTransition : MonoBehaviour
{
    public static SceneTransition Instance { get; private set; }
    [SerializeField] private Animator _transitionAnim;

    public void StartTransition()
    {
        GameManager.Instance.SceneTransition.SetActive(true);
        StartCoroutine(StartTransitionCoroutine());
        GameManager.Instance.ResumeGame();
    }

    IEnumerator StartTransitionCoroutine()
    {
        _transitionAnim.SetTrigger("End");
        yield return new WaitForSeconds(1);
        _transitionAnim.SetTrigger("Start");
    }
}