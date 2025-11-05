using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rascor.App.Helpers;

public static class PreferencesHelper
{
    private const string HasSentRegistrationEmailKey = "has_sent_registration_email";

    public static bool HasSentRegistrationEmail
    {
        get => Preferences.Get(HasSentRegistrationEmailKey, false);
        set => Preferences.Set(HasSentRegistrationEmailKey, value);
    }
}
