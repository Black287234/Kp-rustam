using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace TransportOrders.Web.Controllers;

/// <summary>
/// Сообщения о неверном вводе, которые формирует сам ASP.NET Core (например, буквы в числовом поле).
/// По умолчанию они на английском языке, здесь задаются русские варианты.
/// </summary>
internal static class BindingTexts
{
    public static void Apply(DefaultModelBindingMessageProvider texts)
    {
        texts.SetValueMustNotBeNullAccessor(_ => "Заполните поле");
        texts.SetMissingBindRequiredValueAccessor(field => $"Заполните поле «{field}»");
        texts.SetMissingKeyOrValueAccessor(() => "Значение не указано");
        texts.SetMissingRequestBodyRequiredValueAccessor(() => "Не переданы данные формы");
        texts.SetValueIsInvalidAccessor(value => string.IsNullOrEmpty(value) ? "Заполните поле" : $"Значение «{value}» не подходит");
        texts.SetAttemptedValueIsInvalidAccessor((value, field) => $"«{value}» — неверное значение для поля «{field}»");
        texts.SetNonPropertyAttemptedValueIsInvalidAccessor(value => $"«{value}» — неверное значение");
        texts.SetUnknownValueIsInvalidAccessor(field => $"Неверное значение поля «{field}»");
        texts.SetNonPropertyUnknownValueIsInvalidAccessor(() => "Неверное значение");
        texts.SetValueMustBeANumberAccessor(field => $"В поле «{field}» введите число");
        texts.SetNonPropertyValueMustBeANumberAccessor(() => "Введите число");
    }
}
