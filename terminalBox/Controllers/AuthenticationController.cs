﻿using System;
using System.Threading.Tasks;
using System.Web.Http;
using Data.Interfaces.DataTransferObjects;
using TerminalBase.BaseClasses;

namespace terminalBox.Controllers
{
    [RoutePrefix("authentication")]
    public class AuthenticationController : BaseTerminalController
    {
        //private const string curTerminal = "terminalDropbox";
        //private Authentication _authentication = new Authentication();
        
        //[HttpPost]
        //[Route("initial_url")]
        //public ExternalAuthUrlDTO GenerateOAuthInitiationURL()
        //{
        //    return _authentication.GetExternalAuthUrl();
        //}

        //[HttpPost]
        //[Route("token")]
        //public async Task<AuthorizationTokenDTO> GenerateOAuthToken(
        // ExternalAuthenticationDTO externalAuthDTO)
        //{
        //    try
        //    {
        //        return await _authentication.Authenticate(externalAuthDTO);
        //    }
        //    catch (Exception ex)
        //    {
        //        ReportTerminalError(curTerminal, ex);
        //        return await Task.FromResult(
        //            new AuthorizationTokenDTO()
        //            {
        //                Error = "An error occurred while trying to authorize, please try again later."
        //            }
        //        );
        //    }
        //}
    }
}