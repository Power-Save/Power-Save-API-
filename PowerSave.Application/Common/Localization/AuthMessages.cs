namespace PowerSave.Application.Common.Localization;

public static class AuthMessages
{
    public static string GetArabicMessage(AuthErrorCode code, params object[] args)
    {
        return code switch
        {
            AuthErrorCode.RequiredFieldMissing => "الحقل مطلوب.",
            AuthErrorCode.InvalidPhoneNumber => "رقم الهاتف المحمول غير صالح. يجب أن يكون رقماً مصرياً صالحاً يبدأ بـ 010 أو 011 أو 012 أو 015.",
            AuthErrorCode.InvalidUsernameLength => "يجب أن يتراوح طول اسم المستخدم بين 3 و 20 حرفاً.",
            AuthErrorCode.UnsupportedUsernameCharacters => "يحتوي اسم المستخدم على رموز غير مسموح بها. المسموح به فقط الحروف الإنجليزية والأرقام والرموز (#, _, !).",
            AuthErrorCode.DuplicateUsername => "اسم المستخدم مسجل مسبقاً، يرجى اختيار اسم مستخدم آخر.",
            AuthErrorCode.DuplicatePhoneNumber => "رقم الهاتف مسجل مسبقاً لحساب آخر.",
            AuthErrorCode.PasswordContainsSpaces => "لا يُسمح بوجود مسافات داخل كلمة المرور.",
            AuthErrorCode.PasswordTooShort => "يجب ألا يقل طول كلمة المرور عن 8 خانات.",
            AuthErrorCode.PasswordMissingLetterOrDigit => "يجب أن تحتوي كلمة المرور على حرف إنجليزي واحد على الأقل ورقم واحد على الأقل.",
            AuthErrorCode.WeakPassword => "كلمة المرور ضعيفة وسهلة التخمين. يرجى تجنب تكرار نفس الحرف أو استخدام كلمات المرور الشائعة.",
            AuthErrorCode.MissingRequiredConsent => "الموافقة الإلزامية على إنشاء الحساب وسياسة البيانات مطلوبة للمتابعة.",
            AuthErrorCode.UnsupportedConsentVersion => "إصدار وثيقة الموافقة غير معتمد حالياً.",
            AuthErrorCode.IncorrectOtp => "رمز التحقق المدخل غير صحيح.",
            AuthErrorCode.ExpiredOtp => "انتهت صلاحية رمز التحقق (صلاحية الرمز 5 دقائق).",
            AuthErrorCode.OtpAlreadyUsedOrSuperseded => "رمز التحقق تم استخدامه سابقاً أو تم إلغاؤه لإصدار رمز أحدث.",
            AuthErrorCode.ResendRequestedTooEarly => args.Length > 0
                ? $"يرجى الانتظار {args[0]} ثانية قبل طلب رمز جديد."
                : "يرجى الانتظار 60 ثانية على الأقل بين كل محاولة إرسال.",
            AuthErrorCode.ResendLimitReached => args.Length > 0
                ? $"تم استنفاد محاولات إعادة الإرسال. يمكنك المحاولة مجدداً بعد: {args[0]}"
                : "تم استنفاد محاولات إعادة الإرسال، يرجى الانتظار لحين انتهاء فترة التهدئة.",
            AuthErrorCode.AccountPhoneUnverified => "لم يتم تأكيد رقم الهاتف لهذا الحساب. يرجى تأكيد الهاتف أولاً.",
            AuthErrorCode.AccountInactive => "الحساب غير نشط أو تم حظره.",
            AuthErrorCode.InvalidCredentials => "بيانات الدخول غير صحيحة.",
            AuthErrorCode.UserNotFound => "المستخدم غير موجود.",
            AuthErrorCode.ConcurrencyConflict => "حدث تعارض أثناء معالجة الطلب، يرجى إعادة المحاولة.",
            AuthErrorCode.SmsDeliveryFailed => "تم إنشاء الحساب ولكن تعذر إرسال رسالة التحقق حالياً، يرجى طلب إعادة الإرسال.",
            AuthErrorCode.PasswordMismatch => "كلمة المرور وتأكيد كلمة المرور غير متطابقتين.",
            _ => "حدث خطأ أثناء معالجة الطلب."
        };
    }

    public static string GetEnglishMessage(AuthErrorCode code, params object[] args)
    {
        return code switch
        {
            AuthErrorCode.RequiredFieldMissing => "The field is required.",
            AuthErrorCode.InvalidPhoneNumber => "The phone number is invalid. Must be a valid Egyptian mobile number starting with 010, 011, 012, or 015.",
            AuthErrorCode.InvalidUsernameLength => "Username length must be between 3 and 20 characters.",
            AuthErrorCode.UnsupportedUsernameCharacters => "Username contains invalid characters. Only English letters, digits, and #, _, ! are allowed.",
            AuthErrorCode.DuplicateUsername => "Username is already taken.",
            AuthErrorCode.DuplicatePhoneNumber => "Phone number is already registered to another account.",
            AuthErrorCode.PasswordContainsSpaces => "Password must not contain spaces.",
            AuthErrorCode.PasswordTooShort => "Password must be at least 8 characters long.",
            AuthErrorCode.PasswordMissingLetterOrDigit => "Password must contain at least one English letter and at least one digit.",
            AuthErrorCode.WeakPassword => "Password is too weak. Please avoid repeated single characters or common passwords.",
            AuthErrorCode.MissingRequiredConsent => "Mandatory consents for account creation and data policy are required.",
            AuthErrorCode.UnsupportedConsentVersion => "The provided consent document version is not supported.",
            AuthErrorCode.IncorrectOtp => "The verification code is incorrect.",
            AuthErrorCode.ExpiredOtp => "The verification code has expired (validity is 5 minutes).",
            AuthErrorCode.OtpAlreadyUsedOrSuperseded => "The verification code has already been used or was superseded by a newer code.",
            AuthErrorCode.ResendRequestedTooEarly => args.Length > 0
                ? $"Please wait {args[0]} seconds before requesting a new code."
                : "Please wait at least 60 seconds between resend requests.",
            AuthErrorCode.ResendLimitReached => args.Length > 0
                ? $"Resend limit reached. You can retry after: {args[0]}"
                : "Resend limit reached. Please wait until the cooldown period expires.",
            AuthErrorCode.AccountPhoneUnverified => "Phone number has not been verified yet.",
            AuthErrorCode.AccountInactive => "Account is inactive or blocked.",
            AuthErrorCode.InvalidCredentials => "Invalid credentials.",
            AuthErrorCode.UserNotFound => "User not found.",
            AuthErrorCode.ConcurrencyConflict => "A concurrency conflict occurred. Please retry.",
            AuthErrorCode.SmsDeliveryFailed => "Account created, but SMS delivery failed. Please request an OTP resend.",
            AuthErrorCode.PasswordMismatch => "Password and password confirmation do not match.",
            _ => "An error occurred while processing the request."
        };
    }
}

