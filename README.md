# CoreBus

[![License](https://img.shields.io/github/license/AndreyAbdulkayumov/CoreBus?style=for-the-badge)](https://github.com/AndreyAbdulkayumov/CoreBus/blob/main/LICENSE)
[![GitHub repo size](https://img.shields.io/github/repo-size/AndreyAbdulkayumov/CoreBus?style=for-the-badge)]()
[![Code size](https://img.shields.io/github/languages/code-size/AndreyAbdulkayumov/CoreBus?style=for-the-badge)]()
[![GitHub release (latest by date including pre-releases)](https://img.shields.io/github/v/release/AndreyAbdulkayumov/CoreBus?include_prereleases&style=for-the-badge)](https://github.com/AndreyAbdulkayumov/CoreBus/releases)
[![Downloads](https://img.shields.io/github/downloads/AndreyAbdulkayumov/CoreBus/total?style=for-the-badge)](https://github.com/AndreyAbdulkayumov/CoreBus/releases)
[![Last Commit](https://img.shields.io/github/last-commit/AndreyAbdulkayumov/CoreBus?style=for-the-badge)](https://github.com/AndreyAbdulkayumov/CoreBus/commits/main)

CoreBus — кроссплатформенный терминал для работы с COM-портами и TCP-сокетами с поддержкой протоколов Modbus TCP / RTU / ASCII.

Основные возможности приложения:

1. Два режима работы: "Без протокола" и "Modbus".

2. "Без протокола":
   * Работа с данными в строковом или байтовом формате.
   * Поддержка разных кодировок.
   * Три режима отправки: одиночная, цикличная, отправка файла.

3. "Modbus":
   * Поддержка различных вариаций протокола Modbus: TCP, RTU, ASCII и RTU / ASCII over TCP.
   * Удобная работа с функциями записи.
   * Возможность работы с числами типа float.
   * Возможность работы с бинарными данными.
   * Цикличный опрос.
   * Modbus сканер, который осуществляет поиск устройств на линии связи.

5. Макросы:
   * Отдельные макросы для каждого режима работы.
   * Макрос состоит из неограниченного количества команд (действий).
   * Для Modbus макросов предусмотрена возможность выставления общего Slave ID для всего макроса.
   * Импорт и экспорт макросов.

6. Темная и светлая темы приложения.

7. Пресеты с пользовательскими настройками.

8. Кроссплатформенность: Windows, Linux.

Приложение тестировалось на Windows 10/11, Ubuntu и Astra Linux Common Edition.

## *Помощь проекту*
Автор будет благодарен за любую поддержку. Реквизиты указаны по [ссылке](https://andreyabdulkayumov.github.io/TerminalProgram_Website/donate.html).

## *Статьи на Хабр*

[CoreBus: Часть 5 — попытка использования Native AOT](https://habr.com/ru/articles/922944/)

[Кроссплатформенный терминал Modbus TCP / RTU / ASCII с открытым исходным кодом: Часть 4](https://habr.com/ru/articles/895692/)

[Кроссплатформенный терминал Modbus TCP / RTU / ASCII с открытым исходным кодом: Часть 3](https://habr.com/ru/articles/871788/)

[Кроссплатформенный терминал Modbus TCP / RTU / ASCII с открытым исходным кодом: Часть 2](https://habr.com/ru/articles/854824/)

[Терминал Modbus TCP / RTU / ASCII с открытым исходным кодом: Часть 1](https://habr.com/ru/articles/795387/)

## *История версий*

Ссылки на скачивание любой публичной версии приложения вы можете найти на [этой странице](https://andreyabdulkayumov.github.io/TerminalProgram_Website/downloads.html).

## *Связанные проекты*

[EchoServer](https://github.com/AndreyAbdulkayumov/EchoServer) - консольное приложение для тестирования по TCP/IP.

[ArduinoModbusDevice](https://github.com/AndreyAbdulkayumov/ArduinoModbusDevice) - Modbus RTU Slave устройство. 

# Описание
Начиная с версии 3.0.0 в проекте используется Avalonia UI, в более ранних версиях WPF.

Платформа:
- .NET Framework до версии 1.9.1 включительно.
- .NET 7 начиная с версии 1.10.0.
- .NET 8 начиная с версии 2.3.0.
- .NET 9 начиная с версии 3.0.0.

Ниже будет описание режимов работы приложения. Подробнее можно узнать из статей на Хабр или из встроенного руководства пользователя (кнопка со знаком вопроса в верхнем левом углу). 

## *"Без протокола"*
В поле передачи пользователь пишет данные, которые нужно отправить. 

В поле приема находятся данные, которые прислал сервер или внешнее устройство. 


Можно работать как с байтами, так и со строковыми данными в разных кодировках.


	Поддерживаются протоколы: 
	- UART
	- TCP

	Есть три режима отправки: 
	- Одиночная
	- Цикличная
	- Файлы

<p align="center">
  <img src="https://github.com/user-attachments/assets/481e5cb3-2441-479e-8bca-64cfec122271"/>
</p>

<p align="center">
  <img src="https://github.com/user-attachments/assets/bc960011-efea-4f33-97de-6b6fd97c4724"/>
</p>

## *"Modbus"*
Пользователь может взаимодействовать с выбранными регистрами Modbus, используя соответствующие элементы интерфейса. Для дополнительной расшифровки транзакции существует раздел с представлениями.

	Поддерживаются протоколы: 
	- Modbus TCP
	- Modbus RTU
 	- Modbus ASCII
  	- Modbus RTU over TCP
 	- Modbus ASCII over TCP

<p align="center">
  <img src="https://github.com/user-attachments/assets/311d5f62-ea38-471c-af53-b523bdae8d33"/>
</p>

<p align="center">
  <img src="https://github.com/user-attachments/assets/e09003d1-1f47-49ef-9030-ad838153a2da"/>
</p>

Также в этом режиме реализован Modbus сканер. Он служит для поиска подчиненных устройств на линии связи и доступен только при подключении по последовательному порту.
Список адресов найденных устройств отображается в поле "Устройства".

<p align="center">
  <img src="https://github.com/user-attachments/assets/ffe318a5-bab1-4fe5-9733-cd49e4a7c083"/>
  <img src="https://github.com/user-attachments/assets/3f92c794-e0ab-438d-9ad4-48ff2bf7b61f"/>
</p>

## *"Макросы"*

В приложении предусмотрена работа с макросами. Они доступны для всех режимов. Макросы поддерживают отправку сразу нескольких сообщений за раз. 


Все макросы представлены на рабочем поле в виде кнопок с соответствующими названиями. 
При наведении курсора на любой из макросов появляются кнопки редактирования и удаления.

<p align="center">
  <img src="https://github.com/user-attachments/assets/3c8e565a-d0cb-4d12-b240-11ac016c1abd"/>
</p>

<p align="center">
  <img src="https://github.com/user-attachments/assets/81fee370-8128-40c8-a306-0679d92f1b68"/>
</p>

Макрос разделен на команды. Каждая команда – это отправка одного сообщения.


В режиме редактирования есть возможность отправки отдельных команд или всего макроса полностью. 
Для этого предусмотрены соответствующие кнопки в шапке макроса и у каждой команды в списочной форме.


В макросах Modbus есть возможность использовать единый Slave ID во всем макросе.

<p align="center">
  <img src="https://github.com/user-attachments/assets/55327425-509c-45fc-b15e-aefa56a86972"/>
</p>

<p align="center">
  <img src="https://github.com/user-attachments/assets/aa69326d-b260-44d7-8617-58ef4b9c54c7"/>
</p>

# *Вспомогательный софт*
GUI Framework - [Avalonia UI](https://avaloniaui.net/)

Для упрощения работы с паттерном MVVM использован [ReactiveUI](https://www.reactiveui.net/)

Для тестирования используется [xUnit](https://xunit.net/)

Скрипт установщика написан с помощью [Inno Setup Compiler](https://jrsoftware.org/isdl.php)

Иконки приложения [Material.Icons.Avalonia](https://github.com/AvaloniaUtils/Material.Icons.Avalonia/)

# *Система версирования* Global.Major.Minor

*Global* - глобальная версия репозитория. До релиза это 0. Цифра меняется во время релиза и при именениях, затрагивающих значительную часть UI или внутренней логики.

*Major* - добавление нового функционала, крупные изменения.

*Minor* - исправление багов, мелкие добавления.
