# Публикация на ресурсе Chocolatey

https://community.chocolatey.org/

## Описание файлов

В папке Sources содержатся исходники для создания пакета, публикуемого на ресурсе.

`MainLogo.png` - иконка пакета на ресурсе.

`corebus.nuspec` - файл с информацией о пакете.

`tools/chocolateyinstall.ps1` - скрипт установки приложения.

`tools/chocolateyuninstall.ps1` - скрипт удаления приложения.

`tools/LICENSE.txt` - текст лицензии, взят [отсюда](https://github.com/AndreyAbdulkayumov/CoreBus/blob/master/LICENSE.md)

`tools/VERIFICATION.txt` - инструкция проверки подлинности пакета.

Скрипты установки и удаления написаны с учетом того, что установщик самого приложения создан с использованием **Inno Setup**.

## Публикация

1. Файл установщика `CoreBus_x.x.x_win_x64_installer.exe` (`x.x.x` - номер версии) скопировать в папку 
   `\Deploy\Chocolatey\Sources\tools`.

1. В файле `corebus.nuspec` изменить тег `<version>` на новую версию. При необходимости обновить тег `<description>`.

1. В файле `tools/chocolateyinstall.ps1` изменить:
    * `$fileLocation` - версию приложения.
	* `$checksum` - контрольную сумму.

1. В файле `tools/VERIFICATION.txt` изменить:
	* Ссылку на скачивание в первом пункте.
	* Название исполняемого файла и контрольную сумму во втором пункте. 
	  Контрольная сумма должна совпадать с файлами из других источников!

1. В папке `\Deploy\Chocolatey\Sources` выполнить команду `choco pack`, чтобы собрать публикуемый пакет.
   Он будет называться `corebus.x.x.x.nupkg` (`x.x.x` - номер версии).

1. Протестировать установку / удаление. Открыть **PowerShell** *от имени администратора* и попробовать установить приложение из локального пакета.

	Установка:
	```
	choco install corebus --source="<путь к папке \Deploy\Chocolatey>" -y
	```
	
	Удаление:
	```
	choco uninstall corebus
	```

1. Отправить пакет на ресурс (`x.x.x` - номер версии).

   ```
   choco push corebus.x.x.x.nupkg --source https://push.chocolatey.org/
   ```
   
   Если не получится, то проверить API ключ в личном кабинете.
