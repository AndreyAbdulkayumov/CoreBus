# Валидация полей ввода

Этот функционал используется для валидации данных, введённых пользователем в `TextBox`.

Система построена вокруг интерфейса `INotifyDataErrorInfo` и набора базовых классов/помощников.

Есть два типа сообщений об ошибках:
* **Короткие** - для отобряжением прямо под полем ввода.
* **Полные** - для отображения в сообщениях, логах или где-то еще.


## Основные сущности

* `ValidatedDateInputBase` – базовый класс, реализующий `INotifyDataErrorInfo`. 
  
  Кроме реализации интерфейса содержит:
  * Словарь ошибок `_errors`, где ключ – имя свойства (`propertyName`), значение – `ValidateMessage`.
  * Свойство `HasErrors` показывает, есть ли хоть одна ошибка во всей ViewModel.

* `ValidatedDateInput` – абстрактный класс для реализации во ViewModel с валидируемыми строковыми полями.
  * Наследуется от `ValidatedDateInputBase`.
  * Содержит словарь готовых сообщений об ошибках `AllErrorMessages` (пустое поле, неверный формат числа, IP‑адрес и т.п.).
  * Определяет абстрактный метод  
    `protected abstract ValidateMessage? GetErrorMessage(string fieldName, string? value);`  
    – каждая конкретная ViewModel реализует в нём свою логику проверки.
  * Метод `ValidateInput(string fieldName, string? value)`:
    * вызывается в сеттерах свойств;
    * пересчитывает ошибку для одного свойства и вызывает `OnErrorsChanged(fieldName)`.
  * Метод `GetFullErrorMessage(string propertyName)` – возвращает **полное** текстовое описание ошибки (например, для всплывающих подсказок или диалоговых окон).
  * Метод `ChangeNumberStyleInErrors(string fieldName, NumberStyles style)` – переключает тип сообщения в зависимости от выбранной системы счисления (десятичная/шестнадцатеричная).

* `ValidateMessage` – класс‑контейнер для сообщения об ошибке:
  * `Short` – короткий текст (отображается под полем);
  * `Full` – развёрнутый текст (для подробного описания).

* `IValidationFieldInfo` – интерфейс для получения человекочитаемого названия поля.
  * Метод `string GetFieldViewName(string fieldName)` позволяет, зная имя свойства (`nameof(Property)`), получить текст, который удобно показывать пользователю в диалогах/сообщениях.
  * Реализуется только там, где нужно выводить имя поля, содержащего ошибку.

* `StringValue` – статический помощник для валидации числовых значений и преобразования строки в число.


## Общая схема работы

1. ViewModel, в которой есть валидируемые поля, **наследуется** от `ValidatedDateInput`.
2. Для каждого свойства, которое нужно валидировать, в сеттере вызывается `ValidateInput(nameof(ИмяСвойства), value)`.
3. ViewModel переопределяет метод `GetErrorMessage`, где по имени свойства и его значению решается какое сообщение об ошибке вернуть (или `null`, если всё корректно).
4. UI (через биндинги с поддержкой `INotifyDataErrorInfo`) автоматически отображает краткие сообщения об ошибках под полями ввода.

Таким образом, логика валидации сосредоточена внутри ViewModel, а представление только показывает уже подготовленные текстовые сообщения.


## Как развивать?

В целом функционал валидации не требует доработок. 
Но может возникнуть необходимость в добавлении новых ошибок в словарь `AllErrorMessages` класса `ValidatedDateInput`.
Сделать просто по аналогии.


## Типовые примеры использования

Алгоритм:

1. Наследуемся от `ValidatedDateInput` и при необходимости реализуем интерфейс `IValidationFieldInfo`.
2. Для каждого валидируемого свойства в сеттере вызываем `ValidateInput(nameof(Field), value)` после `RaiseAndSetIfChanged`.

Пример ViewModel с использованием `IValidationFieldInfo`:

```csharp
using ViewModels.Validation;

public class Example_VM : ValidatedDateInput, IValidationFieldInfo
{
    private string? _field;

    public string? Field
    {
        get => _field;
        set
        {
            this.RaiseAndSetIfChanged(ref _field, value);
            ValidateInput(nameof(Field), value);
        }
    }

    private UInt16 _selectedField = 0;

    ...

    #region Валидация

    public string GetFieldViewName(string fieldName)
    {
        switch (fieldName)
        {
            case nameof(Field):
                return "Тестовое поле";

            default:
                return fieldName;
        }
    }

    protected override ValidateMessage? GetErrorMessage(string fieldName, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return AllErrorMessages[NotEmptyField];
        }

        switch (fieldName)
        {
            case nameof(Field):
                return Check_Field(value);
        }

        return null;
    }

    private ValidateMessage? Check_Field(string value)
    {
        if (!StringValue.IsValidNumber(value, NumberStyles.Number, out _selectedField))
        {
            return AllErrorMessages[DecError_UInt16];
        }

        return null;
    }

    #endregion Валидация
}
```


### Вариант без `IValidationFieldInfo`

Если не требуется выводить человекочитаемые имена полей в сообщениях, можно обойтись без `IValidationFieldInfo`:

```csharp
using ViewModels.Validation;

public class Example_VM_NoFieldName : ValidatedDateInput
{
    private string? _field;

    public string? Field
    {
        get => _field;
        set
        {
            this.RaiseAndSetIfChanged(ref _field, value);
            ValidateInput(nameof(Field), value);
        }
    }

    ...

    #region Валидация

    protected override ValidateMessage? GetErrorMessage(string fieldName, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return new ValidateMessage("Введите значение", "Поле не может быть пустым");
        }

        if (!Checker.IsValid(value, out string errorMessage))
        {
            return new ValidateMessage(errorMessage, errorMessage);
        }

        return null;
    }

    #endregion Валидация
}
```