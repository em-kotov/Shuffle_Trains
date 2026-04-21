using System.Collections;
using UnityEngine;

public class AutoLoose : MonoBehaviour
{
    private ParkingRegistrator _parkingRegistrator;
    private SorterRegistrator _sorterRegistrator;

    public void Initialize(ParkingRegistrator parkingRegistrator, SorterRegistrator sorterRegistrator)
    {
        _parkingRegistrator = parkingRegistrator;
        _sorterRegistrator = sorterRegistrator;

        StartCoroutine(StartCheck());
    }

    private IEnumerator StartCheck()
    {
        WaitForSeconds wait = new WaitForSeconds(6f);

        while (true)
        {
            yield return wait;

            CheckParking();
            CheckTrack();
        }
    }

    private void CheckParking()
    {
        bool isPossible = _parkingRegistrator.IsPossibleToMove(out ParkingCar car);

        if (isPossible)
        {
            ShowNextMove("parking");
        }
        else
        {
            ShowText("parking");
        }
    }

    private void CheckTrack()
    {
        bool isPossible = _sorterRegistrator.HaveMoves();

        if (isPossible)
        {
            ShowNextMove("sorter");
        }
        else
        {
            ShowText("sorter");
        }
    }

    private void ShowNextMove(string author)
    {
        Debug.Log($"Auto loose - Still have possible moves - {author}");
    }

    private void ShowText(string author)
    {
        Debug.Log($"Auto loose - Seems you're stuck. Click to restart - {author}");
    }
}
