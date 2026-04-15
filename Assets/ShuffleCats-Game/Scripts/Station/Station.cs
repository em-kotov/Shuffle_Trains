using UnityEngine;

public class Station : MonoBehaviour
{
    [SerializeField] private float _stopNumber;
    [SerializeField] private float _stopNumberExit;

    public float StopNumber()
    {
       return _stopNumber;
    }
    
    public float StopNumberExit()
    {
       return _stopNumberExit;
    }
}
