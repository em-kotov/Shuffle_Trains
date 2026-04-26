using System.Collections;
using UnityEngine;

public class AutoLoose : MonoBehaviour
{
    [SerializeField] private LooseUI _looseUI;

    private ParkingRegistrator _parkingRegistrator;
    private SorterRegistrator _sorterRegistrator;

    public void Initialize(ParkingRegistrator parkingRegistrator,
                        SorterRegistrator sorterRegistrator)
    {
        HideWindow();
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

            if (CanMoveOnParking())
            {
                if (CanSortOnTrack())
                    continue;
            }

            ShowWindow();
        }
    }

    private bool CanMoveOnParking()
    {
        return _parkingRegistrator.IsPossibleToMove(out ParkingCar car);
    }

    private bool CanSortOnTrack()
    {
        return _sorterRegistrator.HaveMoves();
    }

    private void ShowWindow()
    {
        _looseUI.ShowWindow();
    }

    private void HideWindow()
    {
        _looseUI.HideWindow();
    }
}
