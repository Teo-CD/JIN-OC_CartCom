# CartCom

This is a Unity project that communicates with an Arduino board
to transfer [PICO-8](https://www.lexaloffle.com/pico-8.php) cartridges between the computer and an EEPROM.  

It allows connecting to a serial port from the interface, load and write to a cartridge and start a PICO-8 console.  
Thus, __it is necessary__ to provide the path to a PICO-8 executable in the inspector for the full functionality.

The goal here is for students to fill in the blanks regarding the communication between the Arduino and the EEPROM,
and the Arduino and Unity.

The corresponding Arduino code is in :
 - [Arduino/SendCart](Arduino/SendCart), for reading from the EEPROM and sending cartridges to Unity,
 - [Arduino/ReceiveCart](Arduino/ReceiveCart), for receiving cartridges from Unity and writing to the EEPROM.

It has been tested on Unity ***2022.3.9f1***.
