namespace ViewModels.Macros.DataTypes;

internal interface ICommandValidation
{
    string? GetValidationMessage(params FieldNames[] uncheckedFields);
}
