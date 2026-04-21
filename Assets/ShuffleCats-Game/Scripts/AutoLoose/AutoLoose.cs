using System.Collections;
using TMPro;
using UnityEngine;

public class AutoLoose : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _currentText;

    private ParkingRegistrator _parkingRegistrator;
    private SorterRegistrator _sorterRegistrator;

    public void Initialize(ParkingRegistrator parkingRegistrator,
                        SorterRegistrator sorterRegistrator)
    {
        _parkingRegistrator = parkingRegistrator;
        _sorterRegistrator = sorterRegistrator;

        StartCoroutine(StartCheck());
    }

    private IEnumerator StartCheck()
    {
        WaitForSeconds wait = new WaitForSeconds(6f);
        string author = "";

        while (true)
        {
            yield return wait;

            author = "parking";

            if (CanMoveOnParking())
            {
                author = "sorter";

                if (CanMoveOnTrack())
                    continue;
            }

            ShowText(author);
        }
    }

    private bool CanMoveOnParking()
    {
        return _parkingRegistrator.IsPossibleToMove(out ParkingCar car);
    }

    private bool CanMoveOnTrack()
    {
        return _sorterRegistrator.HaveMoves();

        // if (isPossible)
        // {
        //     ShowNextMove("sorter");
        // }
        // else
        // {
        //     ShowText("sorter");
        // }
    }

    // private void ShowNextMove(string author)
    // {
    //     Debug.Log($"Auto loose - Still have possible moves - {author}");
    // }

    private void ShowText(string author)
    {
        _currentText.text = $"Auto loose - Seems you're stuck. " +
                            "Click to restart - {author}";
        //Debug.Log($"Auto loose - Seems you're stuck. 
        // Click to restart - {author}");
    }
}
