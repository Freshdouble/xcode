# Xcode

This project aims to unify the communication interface between embedded systems and computer. This project uses a xml format called xcm (derived from the xtce format used by ESA) to describe the data that is transmitted over the RF link to and from the rocket hardware.
To reach this goal the programs in this project can generate C++ (C++ 17) code which is suitable for deployment on baremetal systems and this repository contains a C# programm which can directly parse such a datastream using the xcm document.

## libxcm

This library performs the task to read and parse the xcm document. It contains the classes that represent the xcm structure as well as an tokenizer to create a C# class structure that represents the xcm document

|          |               |
| -------- | :-----------: |
| Language |      C#       |
| IDE      | Visual Studio |

## libxcmparse

This library contains the code to parse incomming messages in the context of the xcm document. It uses the class structure of libxcm and extends it to handle and parse messages.

|          |               |
| -------- | :-----------: |
| Language |      C#       |
| IDE      | Visual Studio |

## xcmparser

This program allows to parse messages and converting them into a valid json representation. It uses the classes from libxcmparse and feeds them with data over an custom pipe. It then formats the data as json and outputs it to the outputinterface udp server, or directly to the CLI if the verbose option is used.

|          |               |
| -------- | :-----------: |
| Language |      C#       |
| IDE      | Visual Studio |

### Commandline Options

Example xcmparser -f test.xcm -p "serial:/dev/ttyUSB:115200|smp" -v --enableinterface

 > -f, --xcmfile            Required. The xcm file with the package definitions
>
 > -p, --pipe               Required. The pipe expression on how to receive the data
>
 > -o, --outputinterface    (Default: 127.0.0.1:9000) Interface definition for the output UDP Server eg: 127.0.0.1:9000
>
 > -v, --verbose            Enables console data output
>
 > --noforward              Disables the json output except console output
>
 > --enableinterface        Enables a basic text interface that can be opened by pressing the t key in the console window
>
 > --help                   Display this help screen.
>
 > --version                Display version information.

 The pipe expression defines a way to setup a decoding pipline for the downstream interface. The stages are separted by '|' and options within a stage are seperated using ':'.
 The pipeline must always start with a interface. Possible interfaces are

* serial or serialport - This represents a basic connection over a VCP or com port. The syntax is serial:PortName:Baudrate:Parity:Databits:StopBits. The options are a one to one match to the C# serial port class.
* udpreceiver - Used to send and receive udp packets. udpreceiver:LocalEndpointIP:LocalEndpointPort:RemoteEndpointIP:RemoteEndpointPort:SendHearbeat. The last parameter is a boolean indicating that the udpreceiver should send heartbeat packets. If this is set, the transmission is in a protocol which is compatible to the udpserver protocol from libconnection.

After the interface one ore more decoders can be chained together. Possible options are:

* smp - To use libsmp decoding on the datastream

When using the --enableinterface option with the -v option for the verbose output, the command line can be used as a simple interface to receive packets and also send packets.

If the interface is enabled press the 't' key to start the interactive mode. This first screen is an overview over all commands defined inside the xcm file.

Select a command by typing the command number and hit enter.

The next screen shows the command overiew. Where you can see the symbols with all entries and the current values. To change a value type the number of the entrie and hit enter.
Write the new value and hit enter. The view changes back to the command overview but the value should now be updated. To send this command with the set values hit enter without typing anything.

You can change back to the previous window by typing 'e' or "exit" and hitting. If the current window is the top level window this will deactivate the interactive mode.

If the verbose mode is activated the CLI shows the json interpretation of the received data packets, if it is not in interactive mode.

To stop the program press Ctrl+C.

## xcodegen

This program generates the C++ code from the xcm document. It uses the class structure of libxcm and convert it into C++ structures and interface code to send those messages over a datalink.

|          |               |
| -------- | :-----------: |
| Language |      C#       |
| IDE      | Visual Studio |

### Commandline Options


>-f, --xcmfile           Required. The xcm file with the package definitions
>
 > -t, --templatefolder    The folder that contains the templates
>
 > -o, --outputfolder      The folder where to write the output files
>
 > -n, --filename          (Default: xcode) The basefilename (without extension) for the generated output files
>
 > --swap                  Swap messages and commands in the output
>
 > --help                  Display this help screen.
>
 > --version               Display version information.

 ### Dependencies

 The code generated by this program uses the [BaseCom C++ library](https://github.com/Freshdouble/BaseCom) and the [embedded template library](https://github.com/ETLCPP/etl) where the later is included in the BaseCom library as a submodule.

 If you want to send data using the smp transport protocol the C or C++ code of [libsmp](https://github.com/Freshdouble/libsmp) is also needed.

 A sample project to test the connection to NUCLEO-F767ZI board can be found [here](https://github.com/Freshdouble/STM32XCMTest).