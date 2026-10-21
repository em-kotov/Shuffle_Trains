using System.Collections;
using UnityEngine;

public class AutoLoose : MonoBehaviour
{
    [SerializeField] private LooseUI _looseUI;

    private ParkingRegistrator _parkingRegistrator;
    private SorterRegistrator _sorterRegistrator;
    private TrackRegistrator _trackRegistrator;
    // The tutorial explains a real deadlock before opening this same loss UI.
    public bool TutorialOwnsDeadlock { get; set; }

    public void Initialize(ParkingRegistrator parkingRegistrator,
                        SorterRegistrator sorterRegistrator, TrackRegistrator trackRegistrator)
    {
        HideWindow();
        _parkingRegistrator = parkingRegistrator;
        _sorterRegistrator = sorterRegistrator;
        _trackRegistrator = trackRegistrator;

        StartCoroutine(StartCheck());
    }

    private IEnumerator StartCheck()
    {
        WaitForSeconds wait = new WaitForSeconds(6f);

        while (true)
        {
            yield return wait;

            if (!_sorterRegistrator.HasUnfinishedCars)
                yield break;

            bool stalledTrack = _sorterRegistrator.IsTrackDeadlocked(_trackRegistrator.CurrentCount);
            bool fullTrackDeadlock = !_trackRegistrator.IsCountAllows() && stalledTrack;
            bool parkingDeadlock = !_parkingRegistrator.HasMovingCars &&
                !_parkingRegistrator.IsPossibleToMove(out _) &&
                (_trackRegistrator.CurrentCount == 0 || stalledTrack);

            if ((fullTrackDeadlock || parkingDeadlock) && !TutorialOwnsDeadlock)
            {
                ShowWindow();
                yield break;
            }
        }
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
