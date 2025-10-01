using UnityEngine;
using System.Collections;

public class DoorTimerLock : MonoBehaviour
{
    [SerializeField] SlidingDoorController door;
    [SerializeField] float lockSeconds = 10f;
    [SerializeField] bool openIfPlayerWaiting = true;

    IEnumerator Start()
    {
        if (!door) door = GetComponent<SlidingDoorController>();
        if (!door) yield break;

        door.SetMode(SlidingDoorController.Mode.LockedClosed);
        yield return new WaitForSeconds(lockSeconds);
        door.SetMode(SlidingDoorController.Mode.Auto);

        if (openIfPlayerWaiting && door.HasPresence) door.Open();
    }
}
