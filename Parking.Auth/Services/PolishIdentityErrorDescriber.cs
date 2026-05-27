using Microsoft.AspNetCore.Identity;

namespace Parking.Auth.Services;

public sealed class PolishIdentityErrorDescriber : IdentityErrorDescriber
{
  public override IdentityError DefaultError() => new()
  {
    Code = nameof(DefaultError),
    Description = "Wystąpił nieznany błąd."
  };

  public override IdentityError ConcurrencyFailure() => new()
  {
    Code = nameof(ConcurrencyFailure),
    Description = "Wystąpił błąd współbieżności. Spróbuj ponownie."
  };

  public override IdentityError PasswordMismatch() => new()
  {
    Code = nameof(PasswordMismatch),
    Description = "Nieprawidłowe hasło."
  };

  public override IdentityError InvalidToken() => new()
  {
    Code = nameof(InvalidToken),
    Description = "Nieprawidłowy token."
  };

  public override IdentityError LoginAlreadyAssociated() => new()
  {
    Code = nameof(LoginAlreadyAssociated),
    Description = "To logowanie jest już powiązane z innym kontem."
  };

  public override IdentityError InvalidUserName(string? userName) => new()
  {
    Code = nameof(InvalidUserName),
    Description = $"Nazwa użytkownika '{userName}' jest nieprawidłowa."
  };

  public override IdentityError InvalidEmail(string? email) => new()
  {
    Code = nameof(InvalidEmail),
    Description = $"Adres email '{email}' jest nieprawidłowy."
  };

  public override IdentityError DuplicateUserName(string? userName) => new()
  {
    Code = nameof(DuplicateUserName),
    Description = $"Nazwa użytkownika '{userName}' jest już zajęta."
  };

  public override IdentityError DuplicateEmail(string? email) => new()
  {
    Code = nameof(DuplicateEmail),
    Description = $"Adres email '{email}' jest już zajęty."
  };

  public override IdentityError InvalidRoleName(string? role) => new()
  {
    Code = nameof(InvalidRoleName),
    Description = $"Nazwa roli '{role}' jest nieprawidłowa."
  };

  public override IdentityError DuplicateRoleName(string? role) => new()
  {
    Code = nameof(DuplicateRoleName),
    Description = $"Rola '{role}' już istnieje."
  };

  public override IdentityError UserAlreadyHasPassword() => new()
  {
    Code = nameof(UserAlreadyHasPassword),
    Description = "Użytkownik ma już ustawione hasło."
  };

  public override IdentityError UserLockoutNotEnabled() => new()
  {
    Code = nameof(UserLockoutNotEnabled),
    Description = "Blokada konta nie jest włączona dla tego użytkownika."
  };

  public override IdentityError UserAlreadyInRole(string? role) => new()
  {
    Code = nameof(UserAlreadyInRole),
    Description = $"Użytkownik już posiada rolę '{role}'."
  };

  public override IdentityError UserNotInRole(string? role) => new()
  {
    Code = nameof(UserNotInRole),
    Description = $"Użytkownik nie posiada roli '{role}'."
  };

  public override IdentityError PasswordTooShort(int length) => new()
  {
    Code = nameof(PasswordTooShort),
    Description = $"Hasło jest zbyt krótkie. Minimalna długość to {length} znaków."
  };

  public override IdentityError PasswordRequiresNonAlphanumeric() => new()
  {
    Code = nameof(PasswordRequiresNonAlphanumeric),
    Description = "Hasło musi zawierać co najmniej jeden znak specjalny."
  };

  public override IdentityError PasswordRequiresDigit() => new()
  {
    Code = nameof(PasswordRequiresDigit),
    Description = "Hasło musi zawierać co najmniej jedną cyfrę."
  };

  public override IdentityError PasswordRequiresLower() => new()
  {
    Code = nameof(PasswordRequiresLower),
    Description = "Hasło musi zawierać co najmniej jedną małą literę."
  };

  public override IdentityError PasswordRequiresUpper() => new()
  {
    Code = nameof(PasswordRequiresUpper),
    Description = "Hasło musi zawierać co najmniej jedną wielką literę."
  };

  public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) => new()
  {
    Code = nameof(PasswordRequiresUniqueChars),
    Description = $"Hasło musi zawierać co najmniej {uniqueChars} unikalnych znaków."
  };

  public override IdentityError RecoveryCodeRedemptionFailed() => new()
  {
    Code = nameof(RecoveryCodeRedemptionFailed),
    Description = "Nie udało się zrealizować kodu odzyskiwania."
  };
}
