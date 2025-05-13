using TMPro;
using UnityEngine;

public class BookContents : MonoBehaviour
{
    [TextArea(10, 20)][SerializeField] private string content;
    [Space][SerializeField] private TMP_Text leftSide;
    [SerializeField] private TMP_Text rightSide;
    [Space][SerializeField] private TMP_Text leftPagination;
    [SerializeField] private TMP_Text rightPagination;
    [Space][SerializeField] private TMP_Text closeBook;

    private void OnValidate()
    {
        UpdatePagination();

        if (leftSide.text == content)
            return;

        SetupContent();
    }

    private void Awake()
    {
        SetupContent();
        UpdatePagination();
    }

    private void SetupContent()
    {
        leftSide.text = content;
        rightSide.text = content;
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
        this.gameObject.SetActive(false);
    }
}