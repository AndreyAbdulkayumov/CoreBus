# CoreBus

**Language**: [Русский](README.md) | [English](README.en.md)

[![License](https://img.shields.io/github/license/AndreyAbdulkayumov/CoreBus?style=for-the-badge)](https://github.com/AndreyAbdulkayumov/CoreBus/blob/main/LICENSE)
[![GitHub repo size](https://img.shields.io/github/repo-size/AndreyAbdulkayumov/CoreBus?style=for-the-badge)]()
[![Code size](https://img.shields.io/github/languages/code-size/AndreyAbdulkayumov/CoreBus?style=for-the-badge)]()
[![GitHub release (latest by date including pre-releases)](https://img.shields.io/github/v/release/AndreyAbdulkayumov/CoreBus?include_prereleases&style=for-the-badge)](https://github.com/AndreyAbdulkayumov/CoreBus/releases)
[![Downloads](https://img.shields.io/github/downloads/AndreyAbdulkayumov/CoreBus/total?style=for-the-badge)](https://github.com/AndreyAbdulkayumov/CoreBus/releases)
[![Last Commit](https://img.shields.io/github/last-commit/AndreyAbdulkayumov/CoreBus?style=for-the-badge)](https://github.com/AndreyAbdulkayumov/CoreBus/commits/master)

CoreBus is a cross-platform terminal for working with COM ports and TCP sockets, with support for Modbus TCP / RTU / ASCII protocols.

Key features:

1. Three operating modes: "No Protocol", "Modbus", and "Modbus Monitoring".

1. "No Protocol":
   * Work with data in string or byte format.
   * Support a different encodings.
   * Three send modes: single, cyclic, file sending.

1. "Modbus":
   * Support for various Modbus protocol variants: TCP, RTU, ASCII, and RTU / ASCII over TCP.
   * Convenient work with write functions.
   * Support for float numbers.
   * Support for binary data.
   * Modbus scanner that searches for devices on the communication line.
     
1. "Modbus Monitoring":
   * Convenient register display.
   * Conversion to numeric types (Int16/32, float, and others).
   * Transformations using a custom formula.
   * Plotting a chart in real time.
   * Logger.
     
1. Macros:
   * Separate macros for each operating mode.
   * A macro consists of an unlimited number of commands (actions).
   * Modbus macros support setting a common Slave ID for the entire macro.
   * Macro import and export.

1. Dark and light application themes.

1. Presets with user settings.
   
1. User manual (Russian, English).

1. Multi-language interface support: Russian, English, Hindi, Chinese (Simplified), German, Spanish, French, Korean, Portuguese.

1. Cross-platform: Windows, Linux.

The application has been tested on Windows 10/11 and Linux Ubuntu.

## *Demo*
Application version 3.4.0.

"Modbus Monitoring" mode:

[Watch on GitHub (RU)](https://github.com/user-attachments/assets/51c6fbd3-9bad-4fe3-9091-b8a375b0fb89)

Application version 3.3.3.

Working with macros and in "Modbus" and "No Protocol" modes:

[Watch on GitHub (RU)](https://github.com/user-attachments/assets/55d66231-dcb7-4a94-a248-c2b8909a0f87)

You can also watch these videos on [other platforms](https://andreyabdulkayumov.github.io/TerminalProgram_Website/video.html).

## *Where to download?*

The application is available on the following platforms:
* [Official CoreBus website](https://andreyabdulkayumov.github.io/TerminalProgram_Website/downloads.html)

  All public versions and related materials are available here.

* [SourceForge](https://sourceforge.net/projects/corebus/)

	[![Download CoreBus](https://img.shields.io/sourceforge/dt/corebus.svg?style=for-the-badge)](https://sourceforge.net/projects/corebus/files/latest/download)
	
	[![Download CoreBus](https://a.fsdn.com/con/app/sf-download-button)](https://sourceforge.net/projects/corebus/files/latest/download)

* [Chocolatey](https://community.chocolatey.org/packages/corebus)

## *Support the project*
Any support is greatly appreciated. Payment details are available at the [link](https://andreyabdulkayumov.github.io/TerminalProgram_Website/donate.html).

## *Related projects*

[EchoServer](https://github.com/AndreyAbdulkayumov/EchoServer) - a console application for TCP/IP testing.

[ArduinoModbusDevice](https://github.com/AndreyAbdulkayumov/ArduinoModbusDevice) - a Modbus RTU Slave device. 

# Description
Starting with version 3.0.0, the project uses Avalonia UI; earlier versions used WPF.

Platform:
- .NET Framework up to and including version 1.9.1.
- .NET 7 starting with version 1.10.0.
- .NET 8 starting with version 2.3.0.
- .NET 9 starting with version 3.0.0.
- .NET 10 starting with version 3.3.3.

The operating modes are described below. For more details, see the Habr articles or the built-in user guide (the question mark button in the top-left corner). 

## *"No Protocol"*
In the transmit field, the user enters the data to be sent. 

The receive field contains the data received from the server or an external device. 


You can work with both bytes and string data in different encodings.


	Supported protocols: 
	- UART
	- TCP

	There are three send modes: 
	- Single
	- Cyclic
	- Files

<p align="center">
  <img src="https://github.com/user-attachments/assets/6c2d48cd-2c7b-4521-89a3-44589fcd38d5"/>
</p>

<p align="center">
  <img src="https://github.com/user-attachments/assets/85d15ba5-dfe9-4f13-94a3-e7396c9efbce"/>
</p>

## *"Modbus"*
The user can interact with selected Modbus registers using the corresponding UI elements. For additional transaction decoding, there is a representations section.

	Supported protocols: 
	- Modbus TCP
	- Modbus RTU
 	- Modbus ASCII
  	- Modbus RTU over TCP
 	- Modbus ASCII over TCP

<p align="center">
  <img src="https://github.com/user-attachments/assets/15430654-6cc5-464d-826c-7f9762963a6d"/>
</p>

<p align="center">
  <img src="https://github.com/user-attachments/assets/a9222aa1-fd1a-4593-b173-84436a2a0d64"/>
</p>

There are several ways to visualize data:
- Last request.
- Exchange history.
- Binary representation.
- Float number.

<p align="center">
  <img src="https://github.com/user-attachments/assets/bacd6053-50f9-43cd-b311-69278ef31b83"/>
</p>

This mode also includes a Modbus scanner. It is used to find slave devices on the communication line and is available only when connected via a serial port.
The list of discovered device addresses is shown in the "Devices" field.

<p align="center">
  <img src="https://github.com/user-attachments/assets/3b45c96c-596e-4d89-b5df-4bac5ca8a116"/>
</p>

<p align="center">
  <img src="https://github.com/user-attachments/assets/6aee9cb8-0cbf-4dfb-b006-c7232e31049a"/>
</p>

## *"Modbus Monitoring"*
A dedicated mode for visual monitoring of a connected device.

In this mode, Modbus registers are displayed, and their values are updated at a specified interval. 
For convenient viewing, these values can be converted to a selected type, transformed using a formula, and plotted on a chart.

The application can also act as a logger and write selected registers to a file on disk.
<p align="center">
  <img src="https://github.com/user-attachments/assets/f6d8c385-875f-4c7f-87a9-1edaf98cae5c"/>
</p>

<p align="center">
  <img src="https://github.com/user-attachments/assets/d57deaaf-9403-44d9-a2ee-24c418199d55"/>
</p>

## *"Macros"*

The application supports macros. They are available in all modes. Macros allow sending multiple messages at once. 


All macros are shown on the workspace as buttons with corresponding names. 
When you hover over any macro, edit and delete buttons appear.

<p align="center">
  <img src="https://github.com/user-attachments/assets/3c8e565a-d0cb-4d12-b240-11ac016c1abd"/>
</p>

<p align="center">
  <img src="https://github.com/user-attachments/assets/81fee370-8128-40c8-a306-0679d92f1b68"/>
</p>

A macro is divided into commands. Each command is the sending of a single message.


In edit mode, you can send individual commands or the entire macro. 
Corresponding buttons are available in the macro header and next to each command in the list view.


In Modbus macros, you can use a shared Slave ID for the entire macro.

<p align="center">
  <img src="https://github.com/user-attachments/assets/55327425-509c-45fc-b15e-aefa56a86972"/>
</p>

<p align="center">
  <img src="https://github.com/user-attachments/assets/aa69326d-b260-44d7-8617-58ef4b9c54c7"/>
</p>

# *Supporting software*
GUI Framework - [Avalonia UI](https://avaloniaui.net/)

[ReactiveUI](https://www.reactiveui.net/) is used to simplify working with the MVVM pattern

[xUnit](https://xunit.net/) is used for testing

The installer script is written with [Inno Setup Compiler](https://jrsoftware.org/isdl.php)

Application icons: [Material.Icons.Avalonia](https://github.com/AvaloniaUtils/Material.Icons.Avalonia/)
