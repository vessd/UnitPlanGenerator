namespace UnitPlanGenerator.Services.Interfaces
{
    /// <summary>Предоставляет абстракцию для хеширования паролей.</summary>
    public interface IPasswordHasher
    {
        /// <summary>Создает хеш пароля.</summary>
        /// <param name="password">Пользовательский пароль.</param>
        /// <returns>Хешированный пароль.</returns>
        string HashPassword(string password);

        /// <summary>Проверяет, соответствует ли пароль хешу.</summary>
        /// <param name="hashedPassword">Хеш, созданный функцией <see cref="HashPassword(string)"/></param>
        /// <param name="providedPassword">Пользовательский пароль.</param>
        /// <returns>Значение <see langword="true" />, если указанный объект равен текущему объекту; в противном случае — значение <see langword="false" />.</returns>
        bool VerifyPassword(string hashedPassword, string providedPassword);
    }
}
