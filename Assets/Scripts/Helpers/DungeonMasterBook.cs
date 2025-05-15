using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;

public class DungeonMasterBook : MonoBehaviour
{
    public static DungeonMasterBook Instance { get; private set; }
    [SerializeField] private LocalizedString bookContent;
    [Space][SerializeField] private TMP_Text leftSide;
    [SerializeField] private TMP_Text rightSide;
    [Space][SerializeField] private TMP_Text leftPagination;
    [SerializeField] private TMP_Text rightPagination;
    [Space][SerializeField] private TMP_Text closeBook;

    private void OnEnable()
    {
        bookContent.StringChanged += OnContentChanged;
    }

    private void OnDisable()
    {
        bookContent.StringChanged -= OnContentChanged;
    }

    private void OnContentChanged(string localized)
    {
        leftSide.text = localized;
        rightSide.text = localized;
        UpdatePagination();
    }

    private void OnValidate()
    {
        UpdatePagination();
    }

    public void OpenBook()
    {
        Debug.Log("[DungeonMasterBook] Book was opened!");
        this.gameObject.SetActive(true);
        GameManager.Instance.PauseGame();

        // SetupContent();
        UpdatePagination();
    }

    private void UpdatePagination()
    {
        if (rightSide.textInfo.pageCount <= 0)
        {
            return;
        }

        if (rightSide.pageToDisplay >= rightSide.textInfo.pageCount)
        {
            rightPagination.gameObject.SetActive(false);
            closeBook.gameObject.SetActive(true);
        }
        else
        {
            rightPagination.gameObject.SetActive(true);
            closeBook.gameObject.SetActive(false);
        }

        leftPagination.text = leftSide.pageToDisplay.ToString();
        rightPagination.text = rightSide.pageToDisplay.ToString();
    }

    public void PreviousPage()
    {
        if (leftSide.pageToDisplay < 1)
        {
            leftSide.pageToDisplay = 1;
            return;
        }

        if (leftSide.pageToDisplay - 2 > 1)
            leftSide.pageToDisplay -= 2;
        else
            leftSide.pageToDisplay = 1;

        rightSide.pageToDisplay = leftSide.pageToDisplay + 1;

        UpdatePagination();
    }

    public void NextPage()
    {
        if (rightSide.pageToDisplay >= rightSide.textInfo.pageCount)
            return;

        if (leftSide.pageToDisplay >= leftSide.textInfo.pageCount - 1)
        {
            leftSide.pageToDisplay = leftSide.textInfo.pageCount - 1;
            rightSide.pageToDisplay = leftSide.pageToDisplay + 1;
        }
        else
        {
            leftSide.pageToDisplay += 2;
            rightSide.pageToDisplay = leftSide.pageToDisplay + 1;
        }

        UpdatePagination();
    }

    public void CloseBook()
    {
        SceneTransition sceneTransition = GameManager.Instance.GetComponent<SceneTransition>();
        sceneTransition.StartTransition();
        StartCoroutine(CloseBookCoroutine());
    }

    IEnumerator CloseBookCoroutine()
    {
        yield return new WaitForSeconds(1);
        gameObject.SetActive(false);
        Debug.Log("[DungeonMasterBook] Book ended!");
    }
}