using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.XR.CoreUtils;
using UnityEngine.XR;

public class FrontEndSwitcher : MonoBehaviour
{
    [Header("Refs da cena")]
    public XROrigin xrOrigin;
    public Camera frontEndCamera;
    public Canvas frontEndCanvas;
    public Transform startAnchor;
    public UIScreenRouter router;

    [Header("Inputs")]
    public InputActionReference menuAction;
    public InputActionReference startAction;

    bool inFrontEnd = true;

    void Awake() => EnterFrontEnd();

    void OnEnable()
    {
        if (menuAction) { menuAction.action.performed += OnMenu; menuAction.action.Enable(); }
        if (startAction) { startAction.action.performed += OnStart; startAction.action.Enable(); }
    }
    void OnDisable()
    {
        if (menuAction) { menuAction.action.performed -= OnMenu; menuAction.action.Disable(); }
        if (startAction) { startAction.action.performed -= OnStart; startAction.action.Disable(); }
    }

    void OnMenu(InputAction.CallbackContext _)
    {
        if (!inFrontEnd) EnterFrontEnd();
    }

    void OnStart(InputAction.CallbackContext _)
    {
        if (inFrontEnd) EnterGameplay();
    }

    // ===== STATES =====
    public void EnterFrontEnd()
    {
        inFrontEnd = true;

        if (xrOrigin) xrOrigin.gameObject.SetActive(false);
        if (frontEndCamera) frontEndCamera.gameObject.SetActive(true);
        if (frontEndCanvas) frontEndCanvas.gameObject.SetActive(true);

        if (router) router.Resume();        // fecha painéis / Time.timeScale = 1
    }

    public void EnterGameplay()
    {
        inFrontEnd = false;

        if (frontEndCamera) frontEndCamera.gameObject.SetActive(false);
        if (frontEndCanvas) frontEndCanvas.gameObject.SetActive(false);

        if (xrOrigin)
        {
            xrOrigin.gameObject.SetActive(true);
            PlaceRigAtStart();
            RecenterXR();
        }
    }

    void PlaceRigAtStart()
    {
        if (!xrOrigin || !startAnchor) return;
        xrOrigin.MoveCameraToWorldLocation(startAnchor.position);
        xrOrigin.MatchOriginUpCameraForward(startAnchor.up, startAnchor.forward);
    }

    void RecenterXR()
    {
        var subs = new List<XRInputSubsystem>();
        SubsystemManager.GetInstances(subs);
        foreach (var s in subs) s.TryRecenter();
    }
}
