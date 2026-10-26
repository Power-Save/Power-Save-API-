namespace PowerSave.Application.Common.Localization;

public enum AuthErrorCode
{
    None = 0,
    RequiredFieldMissing = 1,
    InvalidPhoneNumber = 2,
    InvalidUsernameLength = 3,
    UnsupportedUsernameCharacters = 4,
    DuplicateUsername = 5,
    DuplicatePhoneNumber = 6,
    PasswordContainsSpaces = 7,
    PasswordTooShort = 8,
    PasswordMissingLetterOrDigit = 9,
    WeakPassword = 10,
    MissingRequiredConsent = 11,
    UnsupportedConsentVersion = 12,
    IncorrectOtp = 13,
    ExpiredOtp = 14,
    OtpAlreadyUsedOrSuperseded = 15,
    ResendRequestedTooEarly = 16,
    ResendLimitReached = 17,
    AccountPhoneUnverified = 18,
    AccountInactive = 19,
    InvalidCredentials = 20,
    UserNotFound = 21,
    ConcurrencyConflict = 22,
    SmsDeliveryFailed = 23,
    GeneralError = 24,
    PasswordMismatch = 25
}

