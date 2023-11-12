using System.Diagnostics;
using System.IO;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;

// This class starts and monitors the PICO-8 process, updating the UI.
public class PICO8_Handler : MonoBehaviour
{
    private enum PicoState
    {
        Inactif,
        Actif,
        Inaccessible
    };

    private PicoState _state = PicoState.Inactif;
    [SerializeField] [NotNull] public string pico8ExePath;
    private Process _pico8 = new ();

    [SerializeField] private UIDocument ui;
    private EnumField _picoState;
    private Button _picoControlButton;
    
    void Start()
    {
        // Connect the UI
        var uiRoot = ui.rootVisualElement;
        _picoControlButton = uiRoot.Query<Button>("PICO-Button");
        _picoControlButton.clickable.clicked += OnButtonClick;
        _picoState = uiRoot.Query<EnumField>("PICO-State");

        // Setup the PICO-8 process : run windowed with the cart in the temporary path.
        _pico8.StartInfo.Arguments = "-windowed 1 -run cart.png";
        _pico8.StartInfo.WorkingDirectory = Application.temporaryCachePath;
        _pico8.StartInfo.FileName = pico8ExePath;
        
        if (!File.Exists(pico8ExePath))
        {
            _state = PicoState.Inaccessible;
            _picoControlButton.SetEnabled(false);
            Debug.LogError("No PICO-8 executable provided !");
        }

        _picoState.value = _state;
    }

    void Update()
    {
        if (_state == PicoState.Inaccessible)
            return;

        // Check if the user has exited the PICO-8 manually
        if (_state == PicoState.Actif && _pico8.HasExited)
        {
            _state = PicoState.Inactif;
            _picoControlButton.text = "Lancer";
        }

        _picoState.value = _state;
    }

    private void OnDestroy()
    {
        if (_state == PicoState.Actif)
        {
            _pico8.Kill();
            _pico8.WaitForExit();
        }
        _picoControlButton.clickable.clicked -= OnButtonClick;
    }

    private void OnButtonClick()
    {
        if (_state == PicoState.Inactif)
        {
            _pico8.Start();
            _state = PicoState.Actif;
            _picoControlButton.text = "Arrêter";
        }
        else if (_state == PicoState.Actif)
        {
            _pico8.Kill();
            _pico8.WaitForExit();
            _state = PicoState.Inactif;
            _picoControlButton.text = "Lancer";
        }
    }
}
