﻿using System;
using System.Threading.Tasks;
using System.Web.Http;
using Data.Interfaces.DataTransferObjects;
using TerminalBase.BaseClasses;
using terminalYammer.Interfaces;
using terminalYammer.Services;

namespace terminalYammer.Controllers
{
    [RoutePrefix("authentication")]
    public class AuthenticationController : BaseTerminalController
    {
        private const string curTerminal = "terminalYammer";

        private readonly IYammer _yammerIntegration;


        public AuthenticationController()
        {
            _yammerIntegration = new Yammer();
        }

        [HttpPost]
        [Route("initial_url")]
        public ExternalAuthUrlDTO GenerateOAuthInitiationURL()
        {
            var externalStateToken = Guid.NewGuid().ToString();
            var url = _yammerIntegration.CreateAuthUrl(externalStateToken);

            var externalAuthUrlDTO = new ExternalAuthUrlDTO()
            {
                ExternalStateToken = externalStateToken,
                Url = url,
            };

            return externalAuthUrlDTO;
        }

        [HttpPost]
        [Route("token")]
        public async Task<AuthorizationTokenDTO> GenerateOAuthToken(
            ExternalAuthenticationDTO externalAuthDTO)
        {
            try
            {
                string code;
                string state;

                ParseCodeAndState(externalAuthDTO.RequestQueryString, out code, out state);

                if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
                {
                    throw new ApplicationException("Code or State is empty.");
                }

                var oauthToken = await _yammerIntegration.GetOAuthToken(code);
                var userID = await _yammerIntegration.GetUserId(oauthToken);

                return new AuthorizationTokenDTO()
                {
                    Token = oauthToken,
                    ExternalStateToken = state,
                    ExternalAccountId = userID
                };
            }
            catch (Exception ex)
            {
                ReportTerminalError(curTerminal, ex);

                return new AuthorizationTokenDTO()
                {
                    Error = "An error occured while trying to authenticate, please try again later."
                };
            }
        }

        private void ParseCodeAndState(string queryString, out string code, out string state)
        {
            if (string.IsNullOrEmpty(queryString))
            {
                throw new ApplicationException("QueryString is empty.");
            }

            code = null;
            state = null;

            var tokens = queryString.Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var token in tokens)
            {
                var nameValueTokens = token.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                if (nameValueTokens.Length < 2)
                {
                    continue;
                }

                if (nameValueTokens[0] == "code")
                {
                    code = nameValueTokens[1];
                }
                else if (nameValueTokens[0] == "state")
                {
                    state = nameValueTokens[1];
                }
            }
        }
    }
}