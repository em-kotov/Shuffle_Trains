using System.Collections;
using UnityEngine;

public class AutoLoose : MonoBehaviour
{
    [SerializeField] private LooseUI _looseUI;

    private ParkingRegistrator _parkingRegistrator;
    private SorterRegistrator _sorterRegistrator;
    private TrackRegistrator _trackRegistrator;
    private Coroutine _checkRoutine;
    private bool _isInitialized;

    public bool TutorialOwnsDeadlock { get; set; }

    private void OnEnable()
    {
        if (_isInitialized)
            StartChecking();
    }

    private void OnDisable()
    {
        StopChecking();
    }

    public void Initialize(ParkingRegistrator parkingRegistrator,
                        SorterRegistrator sorterRegistrator, TrackRegistrator trackRegistrator)
    {
        HideWindow();
        _parkingRegistrator = parkingRegistrator;
        _sorterRegistrator = sorterRegistrator;
        _trackRegistrator = trackRegistrator;
        _isInitialized = true;

        if (isActiveAndEnabled)
            StartChecking();
    }

    private void StartChecking()
    {
        StopChecking();
        _checkRoutine = StartCoroutine(StartCheck());
    }

    private void StopChecking()
    {
        if (_checkRoutine == null)
            return;

        StopCoroutine(_checkRoutine);
        _checkRoutine = null;
    }

    private IEnumerator StartCheck()
    {
        WaitForSeconds wait = new WaitForSeconds(6f);

        while (enabled)
        {
            yield return wait;

            if (_sorterRegistrator.HasUnfinishedCars == false)
                yield break;

            bool stalledTrack = _sorterRegistrator.IsTrackDeadlocked(_trackRegistrator.CurrentCount);
            bool fullTrackDeadlock;

            if (_trackRegistrator.IsCountAllows() == false && stalledTrack)
            {
                fullTrackDeadlock = true;
            }
            else
            {
                fullTrackDeadlock = false;
            }

            bool parkingDeadlock;

            if (_parkingRegistrator.HasMovingCars == false &&
                _parkingRegistrator.IsPossibleToMove(out _) == false &&
                (_trackRegistrator.CurrentCount == 0 || stalledTrack))
            {
                parkingDeadlock = true;
            }
            else
            {
                parkingDeadlock = false;
            }

            if ((fullTrackDeadlock || parkingDeadlock) && TutorialOwnsDeadlock == false)
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
