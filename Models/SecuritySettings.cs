namespace CyberSecurityApp.Models
{
    public class SecuritySettings
    {
        public int Id { get; set; }

        public int MinimumPasswordLength { get; set; } = 15;

        public bool EnforceSequencePolicy { get; set; } = false;

        public int PasswordExpirationDays { get; set; } = 90;

        public int SessionIdleTimeoutMinutes { get; set; } = 20;

        public int OneTimePasswordValidityHours { get; set; } = 24;
    }
}
