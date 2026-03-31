using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Splines;

public class SplineCar : MonoBehaviour
{
    private Mover _mover;
    private Transform _carHead;
    private TrackSwitcher _trackSwitcher;
    private TrackRegistrator _trackRegistrator;
    private SplineAnimate _splineAnimate;
    private SplineContainer _track;
    private Vector3 _currentPosition;
    private Vector3 _enterPoint;
    private bool _isMoving = false;
    private float _interpolatedSplinePosition;
    private float _speed;
    private float _searchMin;
    private float _searchMax;
    private float _waitTime;
    private int _wagonCount;

    public void Initialize(Mover mover, Transform carHead, TrackSwitcher trackSwitcher,
                        SplineAnimate splineAnimate, SplineContainer track, float speed,
                        float searchMin, float searchMax, TrackRegistrator trackRegistrator,
                        float waitTime, int wagonCount = 1)
    {
        _mover = mover;
        _carHead = carHead;
        _trackSwitcher = trackSwitcher;
        _trackRegistrator = trackRegistrator;
        _splineAnimate = splineAnimate;
        _track = track;
        _speed = speed;
        _searchMin = searchMin;
        _searchMax = searchMax;
        _waitTime = waitTime;
        _wagonCount = wagonCount;

        // _trackSwitcher.Initialize(track, trackRegistrator, this);
        _mover.FinishedMoving += OnPausedMoving;
    }

    public void OnBorder()
    {
        if (_isMoving)
            return;

        _currentPosition = _carHead.position;

        _enterPoint = _trackRegistrator.GetEntryPoint(_currentPosition, _searchMin,
                                            _searchMax, out _interpolatedSplinePosition);
        StartCoroutine(SmoothMoveTo(_enterPoint));
    }

    public Vector3 GetPosition()
    {
        return _carHead.position;
    }

    private void OnPausedMoving()
    {
        _isMoving = false;
        _mover.FinishedMoving -= OnPausedMoving;
        // Debug.Log("on paused moving");

        StartCoroutine(WaitFreeEntrance());
    }

    private IEnumerator SmoothMoveTo(Vector3 target)
    {
        _isMoving = true;
        _mover.MoveToPercent(target, 0.1f);
        _carHead.LookAt(target);

        yield return new WaitUntil(() => _isMoving == false);
    }

    private IEnumerator WaitFreeEntrance()
    {
        // WaitForSeconds wait = new(_waitTime);
        // bool isFree = _trackRegistrator.IsSegmentFreeToEnter(_interpolatedSplinePosition, _wagonCount);

        // while (isFree == false)
        // {
        //     yield return wait;
        //     isFree = _trackRegistrator.IsSegmentFreeToEnter( _interpolatedSplinePosition, _wagonCount);
        // }

        _isMoving = true;
        _mover.FinishedMoving -= MovedToTheTarget;
        _mover.FinishedMoving -= OnPausedMoving;
        _mover.FinishedMoving += MovedToTheTarget;
        // Debug.Log("wait free entrance - sending car to move");
        _mover.MoveTo(_enterPoint);
        _carHead.LookAt(_enterPoint);

        yield return new WaitUntil(() => _isMoving == false);

        // _trackRegistrator.Register(_splineAnimate, _carHead);
        EnterSpline();
    }

    private void MovedToTheTarget()
    {
        _isMoving = false;
        // Debug.Log("moved to the target");
        _mover.FinishedMoving -= MovedToTheTarget;
    }

    private void EnterSpline()
    {
        _splineAnimate.enabled = true;
        _splineAnimate.Container = _track;
        _splineAnimate.StartOffset = _interpolatedSplinePosition;
        _splineAnimate.Duration = CalculateDuration(_track.Spline);
        _splineAnimate.Play();
    }

    private float CalculateDuration(ISpline spline)
    {
        return spline.GetLength() / _speed;
    }

    //spherecast methods
    //

    public void OnBumped(Vector3 direction, float distance)
    {
        if (direction == Vector3.forward)
        {
            Debug.Log("Bumped at forward point");

            MoveOneStepBack();
        }

        // Vector3 direction = collisionPoint - otherObjectPosition;

        // float directionSign = Vector3.Dot(direction.normalized, transform.forward);
        // int sign;

        // if (directionSign >= 0)
        // {
        //     sign = 1;
        // }
        // else
        // {
        //     sign = -1;
        // }

        // ChangeT(sign);
    }

    private void ChangeT(int sign)
    {
        //float currentT = _splineAnimate.NormalizedTime % 1f; //get currentT to a [0,1]
        float additionalT = 0.2f * sign;
        // _splineAnimate.NormalizedTime = (_splineAnimate.NormalizedTime + additionalT) % 1f; //looping
        float targetT = (_splineAnimate.NormalizedTime + additionalT) % 1f;
        StartCoroutine(ApplyBump(targetT));
    }

    private IEnumerator ApplyBump(float targetT)
    {
        float elapsed = 0f;
        float startT = _splineAnimate.NormalizedTime;
        float bumpDuration = 0.1f;

        while (elapsed < bumpDuration)
        {
            float t = elapsed / bumpDuration;
            _splineAnimate.NormalizedTime = Mathf.Lerp(startT, targetT, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        _splineAnimate.NormalizedTime = targetT;
        // Mathf.SmoothStep or Vector3.SmoothDamp on position for easing-in/out

        Debug.Log("bump applied");
    }

    private void MoveOneStepBack()
    {
        float stepSize = 0.05f; // Adjust this for a "one step" back
        // Subtract from the current animation time
        _splineAnimate.NormalizedTime = Mathf.Max(0f, _splineAnimate.NormalizedTime - stepSize);
        // The SplineAnimate will continue forward on its own in the next frame
        Debug.Log("bump applied");
    }
}
