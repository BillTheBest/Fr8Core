﻿namespace Data.Interfaces.DataTransferObjects
{
    public class AuthorizationTokenDTO
    {
        public string Token { get; set; }
        public string ExternalAccountId { get; set; }
        public string UserId { get; set; }
        public string ExternalStateToken { get; set; }
        public string AdditionalAttributes { get; set; }
        public string Error { get; set; }
        public bool AuthCompletedNotificationRequired { get; set; }
    }
}
