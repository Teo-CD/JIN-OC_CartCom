using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

// Handle the serial communication with the Arduino,
// the cartridge transfer and related UI interactions.
public class Arduino_Com : MonoBehaviour
{
    private enum ArduinoState
    {
        Déconnecté,
        Connecté,
        Connexion,
        Lecture,
        Écriture,
        Invalide
    }

    // ======== UI ========
    [SerializeField] private UIDocument ui;
    private DropdownField _arduinoSerialPort;
    private EnumField _arduinoState;
    private Button _arduinoControlButton;
    private ProgressBar _arduinoProgress;
    
    private Button _cartLoadButton;
    private Button _cartWriteButton;
    private VisualElement _cartImage;

    // ======== IO ========
    private SerialPort _serial = new ();
    // Probably should be left alone but give the possibility to change it in the inspector.
    [SerializeField] private int baudrate = 115200;
    private string _activeSerialPort;
    private ArduinoState _state = ArduinoState.Déconnecté;

    private string _cartPath;
    private FileStream _cart;
    
    // ======== Cart transfer ========
    // Page data corresponding to the EEPROM.
    private const int PageSize = 0;
    private const int PageCount = 0;
    // Page to be written out.
    private Byte[] _pageBuffer = new byte[PageSize];
    // Current position in _pageBuffer.
    private int _readBytes;
    private int _currentPage;
    
    // Checksum a 128 bytes page of data by summing it into an unsigned 16 bits int,
    // to be used either as a verification with a received checksum or to negate and
    // send as the checksum.
    private static UInt16 ComputeChecksum(Byte[] data)
    {
        if (data.Length != PageSize)
        {
            throw new InvalidDataException("Checksum only accepts "+PageSize+" bytes arrays !");
        }
        
        UInt16 sum = 0;
        for (var i = 0; i < PageSize; i += 2)
        {
            sum += BitConverter.ToUInt16(data, i);
        }
        return sum;
    }
    
    // Core communication function.
    // Receives all the data from the cartridge and writes it out to the png file.
    // As the computer will probably read the serial data faster than the Arduino
    // /and/ the transfer is quite slow (lots of bits, low baudrate), make this a
    // coroutine.
    // This prevents the whole application freezing up why waiting for data and
    // allow presenting visual feedback.
    private IEnumerator<WaitUntil> ReceiveSerialData()
    {
        do
        {
            // We could use an event instead but as Unity is lagging behind .NET versions,
            // it wouldn't be cross-platform.
            yield return new WaitUntil(() => _serial.BytesToRead >= 1);
            
            // Read all available data, up to the PageSize.
            throw new NotImplementedException("Update _readBytes, _pageBuffer with serial data.");

            // Full page received, check checksum and ACK/NAK, write received page.
            if (_readBytes >= PageSize)
            {
                Byte[] checksum = new byte[2];
                // There might be less than the two characters of the checksum available, loop until complete.
                var checksumRead = 0;
                do
                {
                    throw new NotImplementedException(
                        "Update checksumRead, checksum with serial data. The logic is the same as above.");
                } while (checksumRead < 2);
                
                // Implicit type of the sum is 4 bytes, so the overflow doesn't occur. Compute manually.
                if ((UInt16.MaxValue + 1) -
                    (BitConverter.ToUInt16(checksum) + ComputeChecksum(_pageBuffer)) == 0)
                {
                    // Checksum valid, write it out to the cart.
                    _cart.Write(_pageBuffer);
                    _currentPage += 1;
                    throw new NotImplementedException("Confirm good reception over serial to continue");
                }
                else
                {
                    // Checksum invalid, ask for retransmit.
                    throw new NotImplementedException("Ask for retransmission of the page over serial");
                    Debug.LogWarningFormat("Page {0} reception failed, asking for retransmit.", _currentPage);
                }
                _readBytes = 0;
            }
        } while (_currentPage < PageCount);
        
        // We are done receiving the cart, we don't expect more data to be received so the coroutine can return.
        _currentPage = 0;
        _state = ArduinoState.Connecté;
        _cart.Close();
        
        // Re-enable buttons now that the transfer is done.
        _arduinoControlButton.SetEnabled(true);
        _cartLoadButton.SetEnabled(true);
        // _cartWriteButton.SetEnabled(true);  TODO: Not implemented
        
        _arduinoProgress.visible = false;
        
        // Update cart image !
        var newCartTexture = new Texture2D(2, 2);
        var textureData = File.ReadAllBytes(_cartPath);
        newCartTexture.LoadImage(textureData);
        _cartImage.style.backgroundImage = new StyleBackground(newCartTexture);
    }
    
    void Start()
    {
        // ======== UI ========
        var uiRoot = ui.rootVisualElement;
        _arduinoSerialPort = uiRoot.Query<DropdownField>("Arduino-Serial");
        _arduinoSerialPort.RegisterValueChangedCallback(OnNewSerialPort);
        _arduinoSerialPort.choices = new List<string>(SerialPort.GetPortNames());

        _arduinoState = uiRoot.Query<EnumField>("Arduino-State");

        _arduinoControlButton = uiRoot.Query<Button>("Arduino-Button");
        _arduinoControlButton.clickable.clicked += OnArduinoButtonClicked;

        _arduinoProgress = uiRoot.Query<ProgressBar>("Arduino-Progress");
        _arduinoProgress.highValue = PageCount;

        _cartLoadButton = uiRoot.Query<Button>("Cart-Load");
        // Cart buttons can't work if the Arduino is not connected.
        _cartLoadButton.SetEnabled(false);
        _cartLoadButton.clickable.clicked += OnLoadButtonClicked;
        _cartWriteButton = uiRoot.Query<Button>("Cart-Write");
        _cartWriteButton.SetEnabled(false);
        _cartImage = uiRoot.Query<VisualElement>("Cart");

        // ======== IO ========
        _cartPath = Application.temporaryCachePath + "/cart.png";
    }

    void Update()
    {
        // Update the serial ports list so we can detect hot-plugs.
        _arduinoSerialPort.choices.Clear();
        _arduinoSerialPort.choices.AddRange(SerialPort.GetPortNames());
        
        // Update state enum and progress bar if active.
        _arduinoState.value = _state;
        if (_state is ArduinoState.Lecture or ArduinoState.Écriture)
            _arduinoProgress.value = _currentPage;
    }

    private void OnDestroy()
    {
        _arduinoControlButton.clickable.clicked -= OnArduinoButtonClicked;
        _cartLoadButton.clickable.clicked -= OnLoadButtonClicked;
        _arduinoSerialPort.UnregisterValueChangedCallback(OnNewSerialPort);
        
        if (_serial.IsOpen)
            _serial.Close();
        _cart?.Close();
    }

    void OnLoadButtonClicked()
    {
        // Don't allow arduino actions during load.
        _arduinoControlButton.SetEnabled(false);
        _cartLoadButton.SetEnabled(false);
        _cartWriteButton.SetEnabled(false);

        _arduinoProgress.visible = true;
        
        // Clean up eventual previous cartridge.
        if (File.Exists(_cartPath))
            File.Delete(_cartPath);
        // Open the file, ready for transfers.
        _cart = File.OpenWrite(_cartPath);
        
        // Start listening to incoming data asynchronously.
        StartCoroutine(ReceiveSerialData());        
        _serial.Write("GO\n");
        
        _state = ArduinoState.Lecture;
    }

    void OnArduinoButtonClicked()
    {
        if (_state == ArduinoState.Connecté)
        {
            if (_serial.IsOpen)
                _serial.Close();
            _arduinoControlButton.text = "Connecter";
            
            _cartLoadButton.SetEnabled(false);
            _cartWriteButton.SetEnabled(false);
            _arduinoSerialPort.SetEnabled(true);
            
            _state = ArduinoState.Déconnecté;
        }
        else if (_state == ArduinoState.Déconnecté)
        {
            // Set baudrate again here in case it was changed in the inspector.
            _serial.BaudRate = baudrate;
            _serial.PortName = _activeSerialPort;
            _serial.Open();
            if (!_serial.IsOpen)
            {
                Debug.Log("Failed to connect to serial");
                return;
            }
            
            StartCoroutine(DoSerialConnection());
        }
    }
    
    // The serial port should not be changeable when connected.
    void OnNewSerialPort(ChangeEvent<string> newPort)
    {
        if (Equals(newPort.newValue, _activeSerialPort))
            return;
        _activeSerialPort = newPort.newValue;

        // Might be a slight race condition here if a port is removed in-between updates when clicked, so check anyway.
        _state = SerialPort.GetPortNames().Contains(newPort.newValue) ? ArduinoState.Déconnecté : ArduinoState.Invalide;
        _arduinoControlButton.SetEnabled(_state != ArduinoState.Invalide);
    }

    // Update UI during connection and when ready, prevent interacting too quickly.
    IEnumerator<WaitForSeconds> DoSerialConnection()
    {
        _arduinoControlButton.text = "Connexion...";
        _arduinoControlButton.SetEnabled(false);
        _arduinoSerialPort.SetEnabled(false);
        _state = ArduinoState.Connexion;
        
        // Delay enabling transfers because the Arduino is slow to set-up the serial.
        yield return new WaitForSeconds(1.5f);
        
        _arduinoControlButton.text = "Déconnecter";
        _arduinoControlButton.SetEnabled(true);
            
        _cartLoadButton.SetEnabled(true);
        // _cartWriteButton.SetEnabled(true);  TODO: Not implemented

        _state = ArduinoState.Connecté;
    }
}
