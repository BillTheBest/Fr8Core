﻿using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using StructureMap;
using Data.Entities;
using Data.Interfaces;
using Data.Interfaces.DataTransferObjects;
using Data.Infrastructure.StructureMap;
using Hub.Interfaces;
using System.Net.Http;
using System.Security.Claims;
using Microsoft.Owin.Security;
using Microsoft.AspNet.Identity;

namespace HubWeb.Controllers
{
    public class AuthenticationController : ApiController
    {
        private readonly ISecurityServices _security;
        private readonly IAuthorization _authorization;
        private readonly ITerminal _terminal;


        public AuthenticationController()
        {
            _security = ObjectFactory.GetInstance<ISecurityServices>();
            _terminal = ObjectFactory.GetInstance<ITerminal>();
            _authorization = ObjectFactory.GetInstance<IAuthorization>();
        }

        [HttpPost]
        [Fr8ApiAuthorize]
        [ActionName("token")]
        public async Task<IHttpActionResult> Authenticate(CredentialsDTO credentials)
        {
            Fr8AccountDO account;
            TerminalDO terminalDO;

            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                terminalDO = _terminal.GetByKey(credentials.TerminalId);
                account = _security.GetCurrentAccount(uow);
            }

            var response = await _authorization.AuthenticateInternal(
                account,
                terminalDO,
                credentials.Domain,
                credentials.Username,
                credentials.Password,
                credentials.IsDemoAccount
            );

            return Ok(new
            {
                TerminalId =
                    response.AuthorizationToken != null
                        ? response.AuthorizationToken.TerminalID
                        : (int?)null,

                AuthTokenId =
                    response.AuthorizationToken != null
                        ? response.AuthorizationToken.Id.ToString()
                        : null,

                Error = response.Error
            });
        }

        [HttpGet]
        [Fr8ApiAuthorize]
        [ActionName("initial_url")]
        public async Task<IHttpActionResult> GetOAuthInitiationURL(
            [FromUri(Name = "id")] int terminalId)
        {
            Fr8AccountDO account;
            TerminalDO terminal;

            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                terminal = _terminal.GetByKey(terminalId);

                if (terminal == null)
                {
                    throw new ApplicationException("Terminal was not found.");
                }

                account = _security.GetCurrentAccount(uow);
            }

            var externalAuthUrlDTO = await _authorization.GetOAuthInitiationURL(account, terminal);
            return Ok(new { Url = externalAuthUrlDTO.Url });
        }

        [HttpPost]
        public IHttpActionResult Login([FromUri]string username, [FromUri]string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                return BadRequest();
            }

            Request.GetOwinContext().Authentication.SignOut();

            using (IUnitOfWork uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                Fr8AccountDO dockyardAccountDO = uow.UserRepository.FindOne(x => x.UserName == username);
                if (dockyardAccountDO != null)
                {
                    var passwordHasher = new PasswordHasher();
                    if (passwordHasher.VerifyHashedPassword(dockyardAccountDO.PasswordHash, password) ==
                        PasswordVerificationResult.Success)
                    {
                        ISecurityServices security = ObjectFactory.GetInstance<ISecurityServices>();
                        ClaimsIdentity identity = security.GetIdentity(uow, dockyardAccountDO);
                        Request.GetOwinContext().Authentication.SignIn(new AuthenticationProperties
                        {
                            IsPersistent = true
                        }, identity);
                    }
                }
            }
            return StatusCode(System.Net.HttpStatusCode.Forbidden);
        }
    }
}