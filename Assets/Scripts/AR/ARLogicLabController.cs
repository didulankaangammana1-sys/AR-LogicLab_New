
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARLogicLabController : MonoBehaviour
{
    [Header("AR Setup")]
    [SerializeField] private ARRaycastManager raycastManager;
    [SerializeField] private GameObject boardPrefab;

    [Header("Buttons")]
    [SerializeField] private Button inputAButton;
    [SerializeField] private Button inputBButton;
    [SerializeField] private Button resetButton;

    [Header("UI Text")]
    [SerializeField] private TMP_Text inputAText;
    [SerializeField] private TMP_Text inputBText;
    [SerializeField] private TMP_Text outputText;
    [SerializeField] private TMP_Text statusText;

    private bool inputA = false;
    private bool inputB = false;

    private GameObject placedBoard;
    private LogicGateVisual gateVisual;

    private static readonly List<ARRaycastHit> hits =
        new List<ARRaycastHit>();

    private void Start()
    {
        inputAButton.onClick.AddListener(ToggleA);
        inputBButton.onClick.AddListener(ToggleB);
        resetButton.onClick.AddListener(ResetBoard);

        statusText.text = "Move your phone to scan a table";
        UpdateLogic();
    }

    private void Update()
    {
        if (placedBoard != null)
            return;

        if (Input.touchCount == 0)
            return;

        Touch touch = Input.GetTouch(0);

        if (touch.phase != TouchPhase.Began)
            return;

        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject(
                touch.fingerId))
            return;

        if (raycastManager.Raycast(
            touch.position,
            hits,
            TrackableType.PlaneWithinPolygon))
        {
            Pose pose = hits[0].pose;

            placedBoard = Instantiate(
                boardPrefab,
                pose.position,
                pose.rotation
            );

            gateVisual =
                placedBoard.GetComponent<LogicGateVisual>();

            statusText.text =
                "AND Gate Ready! Change the inputs.";

            UpdateLogic();
        }
    }

    private void ToggleA()
    {
        inputA = !inputA;
        UpdateLogic();
    }

    private void ToggleB()
    {
        inputB = !inputB;
        UpdateLogic();
    }

    private void UpdateLogic()
    {
        bool output = inputA && inputB;

        inputAText.text = "A: " + (inputA ? "1" : "0");
        inputBText.text = "B: " + (inputB ? "1" : "0");

        outputText.text =
            "OUTPUT Y = " + (output ? "1" : "0");

        if (gateVisual != null)
            gateVisual.SetOutput(output);
    }

    private void ResetBoard()
    {
        if (placedBoard != null)
            Destroy(placedBoard);

        placedBoard = null;
        gateVisual = null;

        inputA = false;
        inputB = false;

        statusText.text =
            "Scan a surface and tap to place the board";

        UpdateLogic();
    }
}
