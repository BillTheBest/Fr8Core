﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Data.Interfaces.DataTransferObjects;

namespace terminalYammer.Interfaces
{
    public interface IYammer
    {
        string CreateAuthUrl(string externalStateToken);
        Task<string> GetOAuthToken(string code);
        Task<string> GetUserId(string oauthToken);
        Task<List<FieldDTO>> GetGroupsList(string oauthToken);
        Task<bool> PostMessageToGroup(string oauthToken, string groupId, string message);
    }
}
