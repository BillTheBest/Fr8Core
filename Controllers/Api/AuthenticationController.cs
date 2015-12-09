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
using Hub.Services;

namespace HubWeb.Controllers
{
    public class AuthenticationController : ApiController
    {
        private readonly ISecurityServices _security;
        private readonly Authorization _authorization;


        public AuthenticationController()
        {
            _security = ObjectFactory.GetInstance<ISecurityServices>();
            _authorization = new Authorization();
        }

        [HttpPost]
        [Fr8ApiAuthorize]
        [ActionName("token")]
        public async Task<IHttpActionResult> Authenticate(CredentialsDTO credentials)
        {
            Fr8AccountDO account;
            ActionDO actionDO;

            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                actionDO = uow.ActionRepository
                    .GetQuery()
                    .Include(x => x.ActivityTemplate)
                    .Include(x => x.ActivityTemplate.Terminal)
                    .SingleOrDefault(x => x.Id == credentials.ActionId);

                if (actionDO == null)
                {
                    throw new ApplicationException("ActivityTemplate was not found.");
                }

                account = _security.GetCurrentAccount(uow);
            }

            var error = await _authorization.AuthenticateInternal(
                account,
                actionDO,
                credentials.Domain,
                credentials.Username,
                credentials.Password);

            return Ok(new { Error = error });
        }

        [HttpGet]
        [Fr8ApiAuthorize]
        [ActionName("initial_url")]
        public async Task<IHttpActionResult> GetOAuthInitiationURL(
            [FromUri(Name = "id")] Guid actionId)
        {
            Fr8AccountDO account;
            ActionDO actionDO;

            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                actionDO = uow.ActionRepository
                    .GetQuery()
                    .Include(x => x.ActivityTemplate)
                    .Include(x => x.ActivityTemplate.Terminal)
                    .SingleOrDefault(x => x.Id == actionId);

                if (actionDO == null)
                {
                    throw new ApplicationException("ActionDO was not found.");
                }

                account = _security.GetCurrentAccount(uow);
            }

            var externalAuthUrlDTO = await _authorization.GetOAuthInitiationURL(account, actionDO);
            return Ok(new { Url = externalAuthUrlDTO.Url });
        }
    }
}