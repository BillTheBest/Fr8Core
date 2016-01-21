﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using Data.Constants;
using Data.Control;
using Data.Infrastructure;
using StructureMap;
using Newtonsoft.Json;
using Data.Crates;
using Data.Entities;
using Data.Interfaces;
using Data.Interfaces.DataTransferObjects;
using Data.Interfaces.Manifests;
using Data.States;
using Hub.Interfaces;
using Hub.Managers;
using Hub.Managers.APIManagers.Transmitters.Restful;

namespace Hub.Services
{
    public class Authorization : IAuthorization
    {
        private readonly ICrateManager _crate;
	    private readonly ITime _time;
        private readonly IActivityTemplate _activityTemplate;
        private readonly ITerminal _terminal;


        public Authorization()
        {
            _terminal = ObjectFactory.GetInstance<ITerminal>();
			_crate = ObjectFactory.GetInstance<ICrateManager>();
	        _time = ObjectFactory.GetInstance<ITime>();
            _activityTemplate = ObjectFactory.GetInstance<IActivityTemplate>();
        }

        public string GetToken(string userId, int terminalId)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var curAuthToken = uow.AuthorizationTokenRepository.FindToken(userId, terminalId, AuthorizationTokenState.Active);

                if (curAuthToken != null)
                    return curAuthToken.Token;
            }
            return null;
        }

//        public string GetTerminalToken(int terminalId)
//        {
//            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
//            {
//                var curAuthToken = uow.AuthorizationTokenRepository.FindOne(at =>
//                    at.TerminalID == terminalId
//                    && at.AuthorizationTokenState == AuthorizationTokenState.Active);
//
//                if (curAuthToken != null)
//                    return curAuthToken.Token;
//            }
//            return null;
//        }

        /// <summary>
        /// Prepare AuthToken for ActionDTO request message.
        /// </summary>
        public void PrepareAuthToken(ActionDTO actionDTO)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                // Fetch ActivityTemplate.
                var activityTemplate = _activityTemplate.GetByKey(actionDTO.ActivityTemplateId.Value);
                    
                // Fetch Action.
                var action = uow.ActionRepository.GetByKey(actionDTO.Id);
                if (action == null)
                {
                    throw new ApplicationException("Could not find Action.");
                }

                // Try to find AuthToken if terminal requires authentication.
                if (activityTemplate.NeedsAuthentication &&
                    activityTemplate.Terminal.AuthenticationType != AuthenticationType.None)
                {
                    // Try to get owner's account for Action -> Route.
                    // Can't follow guideline to init services inside constructor. 
                    // Current implementation of Route and Action services are not good and are depedant on each other.
                    // Initialization of services in constructor will cause stack overflow
                    var route = ObjectFactory.GetInstance<IRoute>().GetRoute(action);
                    var dockyardAccount = route != null ? route.Fr8Account : null;

                    if (dockyardAccount == null)
                    {
                        throw new ApplicationException("Could not find DockyardAccount for Action's Route.");
                    }

                    var accountId = dockyardAccount.Id;

                    // Try to find AuthToken for specified terminal and account.
                    // var authToken = uow.AuthorizationTokenRepository
                    //     .FindOne(x => x.Terminal.Id == activityTemplate.Terminal.Id
                    //         && x.UserDO.Id == accountId);
                    
                    var actionDO = uow.ActionRepository.GetByKey(actionDTO.Id);
                    if (actionDO == null)
                    {
                        throw new ApplicationException("Could not find ActionDO for Action's RouteNode.");
                    }

                    AuthorizationTokenDO authToken = null;
                    if (actionDO.AuthorizationTokenId.HasValue)
                    {
                        authToken = uow.AuthorizationTokenRepository
                            .FindTokenById(actionDO.AuthorizationTokenId.ToString());
                    }

                    // If AuthToken is not empty, fill AuthToken property for ActionDTO.
                    if (authToken != null && !string.IsNullOrEmpty(authToken.Token))
                    {
                        actionDTO.AuthToken = new AuthorizationTokenDTO()
                        {
                            Id = authToken.Id.ToString(),
                            UserId = authToken.UserDO != null ? authToken.UserDO.Id : authToken.UserID,
                            Token = authToken.Token,
                            AdditionalAttributes = authToken.AdditionalAttributes
                        };
                    }
                }

                if (actionDTO.AuthToken == null)
                {
                    var route = ObjectFactory.GetInstance<IRoute>().GetRoute(action);
                    var dockyardAccount = route != null ? route.Fr8Account : null;

                    if (dockyardAccount != null)
                    {
                        actionDTO.AuthToken = new AuthorizationTokenDTO
                        {
                            UserId = dockyardAccount.Id,
                        };
                    }
                }
            }
        }

        public async Task<AuthenticateResponse> AuthenticateInternal(
            Fr8AccountDO account,
            TerminalDO terminal,
            string domain,
            string username,
            string password)
        {
            if (terminal.AuthenticationType == AuthenticationType.None)
            {
                throw new ApplicationException("Terminal does not require authentication.");
            }

            var restClient = ObjectFactory.GetInstance<IRestfulServiceClient>();

            var credentialsDTO = new CredentialsDTO()
            {
                Domain = domain,
                Username = username,
                Password = password
            };

            var terminalResponse = await restClient.PostAsync<CredentialsDTO>(
                new Uri("http://" + terminal.Endpoint + "/authentication/internal"),
                credentialsDTO
            );

            var terminalResponseAuthTokenDTO = JsonConvert.DeserializeObject<AuthorizationTokenDTO>(terminalResponse);
            if (!string.IsNullOrEmpty(terminalResponseAuthTokenDTO.Error))
            {
                return new AuthenticateResponse()
                {
                    Error = terminalResponseAuthTokenDTO.Error
                };
            }

            if (terminalResponseAuthTokenDTO == null)
            {
                return new AuthenticateResponse()
                {
                    Error = "An error occured while authenticating, please try again."
                };
            }

            var curTerminal = _terminal.GetByKey(terminal.Id);

            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var curAccount = uow.UserRepository.GetByKey(account.Id);

                AuthorizationTokenDO authToken = null;
                if (!string.IsNullOrEmpty(terminalResponseAuthTokenDTO.ExternalAccountId))
                {
                    authToken = uow.AuthorizationTokenRepository
                        .GetPublicDataQuery()
                        .FirstOrDefault(x => x.TerminalID == curTerminal.Id
                            && x.UserID == curAccount.Id
                            && x.ExternalAccountId == terminalResponseAuthTokenDTO.ExternalAccountId
                        );
                }

                if (authToken == null)
                {
                    authToken = new AuthorizationTokenDO()
                    {
                        Token = terminalResponseAuthTokenDTO.Token,
                        ExternalAccountId = terminalResponseAuthTokenDTO.ExternalAccountId,
                        Terminal = curTerminal,
                        UserDO = curAccount,
                        ExpiresAt = DateTime.Today.AddMonths(1)
                    };

                    uow.AuthorizationTokenRepository.Add(authToken);
                }
                else
                {
                    authToken.Token = terminalResponseAuthTokenDTO.Token;
                    authToken.ExternalAccountId = terminalResponseAuthTokenDTO.ExternalAccountId;
                }

                uow.SaveChanges();

                //if terminal requires Authentication Completed Notification, follow the existing terminal event notification protocol 
                //to notify the terminal about authentication completed event
                if (terminalResponseAuthTokenDTO.AuthCompletedNotificationRequired)
                {
                    EventManager.TerminalAuthenticationCompleted(curAccount.Id, curTerminal);
                }

                return new AuthenticateResponse()
                {
                    AuthorizationToken = authToken,
                    Error = null
                };
            }
        }

        public async Task<AuthenticateResponse> GetOAuthToken(
            TerminalDO terminal,
            ExternalAuthenticationDTO externalAuthDTO)
        {
            var hasAuthentication = _activityTemplate.GetQuery().Any(x => x.Terminal.Id == terminal.Id);

            if (!hasAuthentication)
            {
                throw new ApplicationException("Terminal does not require authentication.");
            }

            var restClient = ObjectFactory.GetInstance<IRestfulServiceClient>();

            var response = await restClient.PostAsync<ExternalAuthenticationDTO>(
                new Uri("http://" + terminal.Endpoint + "/authentication/token"),
                externalAuthDTO
                );

            var authTokenDTO = JsonConvert.DeserializeObject<AuthorizationTokenDTO>(response);
            if (!string.IsNullOrEmpty(authTokenDTO.Error))
            {
                return new AuthenticateResponse()
                {
                    Error = authTokenDTO.Error
                };
            }

            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var authTokenByExternalState = uow.AuthorizationTokenRepository
                    .FindTokenByExternalState(authTokenDTO.ExternalStateToken, terminal.Id);

                if (authTokenByExternalState == null)
                {
                    throw new ApplicationException("No AuthToken found with specified ExternalStateToken.");
                }

                var authTokenByExternalAccountId = uow.AuthorizationTokenRepository
                    .FindTokenByExternalAccount(
                        authTokenDTO.ExternalAccountId,
                        terminal.Id,
                        authTokenByExternalState.UserID
                    );

                if (authTokenByExternalAccountId != null)
                {
                    authTokenByExternalAccountId.Token = authTokenDTO.Token;
                    authTokenByExternalState.ExternalAccountId = authTokenDTO.ExternalAccountId;
                    authTokenByExternalAccountId.ExternalStateToken = null;
                    authTokenByExternalState.AdditionalAttributes = authTokenDTO.AdditionalAttributes;

                    uow.AuthorizationTokenRepository.Remove(authTokenByExternalState);
                }
                else
                {
                    authTokenByExternalState.Token = authTokenDTO.Token;
                    authTokenByExternalState.ExternalAccountId = authTokenDTO.ExternalAccountId;
                    authTokenByExternalState.ExternalStateToken = null;
                    authTokenByExternalState.AdditionalAttributes = authTokenDTO.AdditionalAttributes;
                }

                uow.SaveChanges();

                return new AuthenticateResponse()
                {
                    AuthorizationToken = authTokenByExternalAccountId ?? authTokenByExternalState,
                    Error = null
                };
            }
        }


        public async Task<ExternalAuthUrlDTO> GetOAuthInitiationURL(
            Fr8AccountDO user,
            TerminalDO terminal)
        {
            if (terminal.AuthenticationType == AuthenticationType.None)
            {
                throw new ApplicationException("Terminal does not require authentication.");
            }

            var restClient = ObjectFactory.GetInstance<IRestfulServiceClient>();

            var response = await restClient.PostAsync(
                new Uri("http://" + terminal.Endpoint + "/authentication/initial_url")
            );

            var externalAuthUrlDTO = JsonConvert.DeserializeObject<ExternalAuthUrlDTO>(response);

            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var authToken = uow.AuthorizationTokenRepository
                    .GetPublicDataQuery()
                    .FirstOrDefault(x => x.TerminalID == terminal.Id
                        && x.UserID == user.Id
                        && x.ExternalAccountId == null
                        && x.ExternalStateToken != null
                    );

                if (authToken == null)
                {
                    var curTerminal = _terminal.GetByKey(terminal.Id);
                    var curAccount = uow.UserRepository.GetByKey(user.Id);

                    authToken = new AuthorizationTokenDO()
                    {
                        UserDO = curAccount,
                        Terminal = curTerminal,
                        ExpiresAt = DateTime.Today.AddMonths(1),
                        ExternalStateToken = externalAuthUrlDTO.ExternalStateToken
                    };

                    uow.AuthorizationTokenRepository.Add(authToken);
                }
                else
                {
                    authToken.ExternalAccountId = null;
                    authToken.Token = null;
                    authToken.ExternalStateToken = externalAuthUrlDTO.ExternalStateToken;
                }

                uow.SaveChanges();
            }

            return externalAuthUrlDTO;
        }

        public void AddAuthenticationCrate(ActionDTO actionDTO, int authType)
        {
            using (var updater = _crate.UpdateStorage(() => actionDTO.CrateStorage))
            {
                AuthenticationMode mode = authType == AuthenticationType.Internal ? AuthenticationMode.InternalMode : AuthenticationMode.ExternalMode;

                switch (authType)
                {
                    case AuthenticationType.Internal:
                        mode = AuthenticationMode.InternalMode;
                        break;
                    case AuthenticationType.External:
                        mode = AuthenticationMode.ExternalMode;
                        break;
                    case AuthenticationType.InternalWithDomain:
                        mode = AuthenticationMode.InternalModeWithDomain;
                        break;
                    case AuthenticationType.None:
                    default:
                        mode = AuthenticationMode.ExternalMode;
                        break;
                }

                updater.CrateStorage.Add(_crate.CreateAuthenticationCrate("RequiresAuthentication", mode));
            }
        }

        public void RemoveAuthenticationCrate(ActionDTO actionDTO)
        {
            using (var updater = _crate.UpdateStorage(() => actionDTO.CrateStorage))
            {
                updater.CrateStorage.RemoveByManifestId((int) MT.StandardAuthentication);
            }
        }

        private void AddAuthenticationLabel(ActionDTO actionDTO)
        {
            using (var updater = _crate.UpdateStorage(actionDTO))
            {
                var controlsCrate = updater.CrateStorage
                    .CratesOfType<StandardConfigurationControlsCM>()
                    .FirstOrDefault();

                if (controlsCrate == null)
                {
                    controlsCrate = Crate<StandardConfigurationControlsCM>
                        .FromContent("Configuration_Controls", new StandardConfigurationControlsCM());

                    updater.CrateStorage.Add(controlsCrate);
                }

                controlsCrate.Content.Controls.Add(
                    new TextBlock()
                    {
                        Name = "AuthAwaitLabel",
                        Value = "Please provide credentials to access your desired account"
                    });
            }
        }

        private void RemoveAuthenticationLabel(ActionDTO actionDTO)
        {
            using (var updater = _crate.UpdateStorage(actionDTO))
            {
                var controlsCrate = updater.CrateStorage
                    .CratesOfType<StandardConfigurationControlsCM>()
                    .FirstOrDefault();
                if (controlsCrate == null) { return; }

                var authAwaitLabel = controlsCrate.Content.FindByName("AuthAwaitLabel");
                if (authAwaitLabel == null) { return; }

                controlsCrate.Content.Controls.Remove(authAwaitLabel);

                if (controlsCrate.Content.Controls.Count == 0)
                {
                    updater.CrateStorage.Remove(controlsCrate);
                }
            }
        }

        public bool ValidateAuthenticationNeeded(string userId, ActionDTO curActionDTO)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var activityTemplate = _activityTemplate.GetByKey(curActionDTO.ActivityTemplateId.Value);

                if (activityTemplate == null)
                {
                    throw new NullReferenceException("ActivityTemplate was not found.");
                }

                var account = uow.UserRepository.GetByKey(userId);

                if (account == null)
                {
                    throw new NullReferenceException("Current account was not found.");
                }

                if (activityTemplate.Terminal.AuthenticationType != AuthenticationType.None
                    && activityTemplate.NeedsAuthentication)
                {
                    RemoveAuthenticationCrate(curActionDTO);
                    RemoveAuthenticationLabel(curActionDTO);

                    var actionDO = uow.ActionRepository.GetByKey(curActionDTO.Id);
                    if (actionDO == null)
                    {
                        throw new NullReferenceException("Current action was not found.");
                    }

                    AuthorizationTokenDO authToken = null;

                    // Check if action has assigned auth-token.
                    if (actionDO.AuthorizationTokenId != null)
                    {
                        authToken = uow.AuthorizationTokenRepository
                            .FindTokenById(actionDO.AuthorizationTokenId.Value.ToString());
                    }

                    // If action does not have assigned auth-token,
                    // then look for AuthToken with IsMain == true,
                    // and assign that token to action.
                    else
                    {
                        var mainAuthTokenId = uow.AuthorizationTokenRepository
                            .GetPublicDataQuery()
                            .Where(x => x.UserID == userId
                                && x.TerminalID == activityTemplate.Terminal.Id
                                && x.IsMain == true)
                            .Select(x => (Guid?)x.Id)
                            .FirstOrDefault();

                        if (mainAuthTokenId.HasValue)
                        {
                            authToken = uow.AuthorizationTokenRepository
                                .FindTokenById(mainAuthTokenId.Value.ToString());
                        }

                        if (authToken != null)
                        {
                            actionDO.AuthorizationToken = authToken;
                            uow.SaveChanges();
                        }
                    }

                    // FR-1958: remove token if could not extract secure data.
                    if (authToken != null && string.IsNullOrEmpty(authToken.Token))
                    {
                        RemoveToken(uow, authToken);
                        authToken = null;
                    }

                    if (authToken == null)
                    {
                        AddAuthenticationCrate(curActionDTO, activityTemplate.Terminal.AuthenticationType);
                        AddAuthenticationLabel(curActionDTO);

                        return true;
                    }
                }
            }

            return false;
        }

        public void InvalidateToken(string userId, ActionDTO curActionDto)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var activityTemplate = _activityTemplate.GetByKey(curActionDto.ActivityTemplateId.Value);

                if (activityTemplate == null)
                {
                    throw new NullReferenceException("ActivityTemplate was not found.");
                }

                var account = uow.UserRepository.GetByKey(userId);

                if (account == null)
                {
                    throw new NullReferenceException("Current account was not found.");
                }

                if (activityTemplate.Terminal.AuthenticationType != AuthenticationType.None
                    && activityTemplate.NeedsAuthentication)
                {
                    var actionDO = uow.ActionRepository.GetByKey(curActionDto.Id);
                    if (actionDO == null)
                    {
                        throw new NullReferenceException("Current action was not found.");
                    }

                    var token = actionDO.AuthorizationToken;

                    // var token = uow.AuthorizationTokenRepository
                    //     .FindOne(x => x.Terminal.Id == activityTemplate.Terminal.Id && x.UserDO.Id == account.Id);
                    
                    if (token != null)
                    {
                        actionDO.AuthorizationToken = null;
                        uow.SaveChanges();

                        uow.AuthorizationTokenRepository.Remove(token);
                        uow.SaveChanges();
                    }

                    RemoveAuthenticationCrate(curActionDto);
                    RemoveAuthenticationLabel(curActionDto);

                    AddAuthenticationCrate(curActionDto, activityTemplate.Terminal.AuthenticationType);
                    AddAuthenticationLabel(curActionDto);
                }
            }
        }

        public IEnumerable<AuthorizationTokenDO> GetAllTokens(string accountId)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var authTokens = uow.AuthorizationTokenRepository
                    .GetPublicDataQuery()
                    .Where(x => x.UserID == accountId)
                    .OrderBy(x => x.ExternalAccountId)
                    .ToList();

                return authTokens;
            }
        }

        public void GrantToken(Guid actionId, Guid authTokenId)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var action = uow.ActionRepository.GetByKey(actionId);
                if (action == null)
                {
                    throw new ApplicationException("Could not find specified Action.");
                }

                var authToken = uow.AuthorizationTokenRepository.FindTokenById(authTokenId.ToString());
                if (authToken == null)
                {
                    throw new ApplicationException("Could not find specified AuthToken.");
                }

                action.AuthorizationToken = authToken;

                uow.SaveChanges();
            }
        }

        public void RevokeToken(string accountId, Guid authTokenId)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var authToken = uow.AuthorizationTokenRepository
                    .GetPublicDataQuery()
                    .Where(x => x.UserID == accountId && x.Id == authTokenId)
                    .SingleOrDefault();

                if (authToken != null)
                {
                    RemoveToken(uow, authToken);
                }
            }
        }

        private void RemoveToken(IUnitOfWork uow, AuthorizationTokenDO authToken)
        {
                    var actions = uow.ActionRepository
                        .GetQuery()
                        .Where(x => x.AuthorizationToken.Id == authToken.Id)
                        .ToList();

                    foreach (var action in actions)
                    {
                        action.AuthorizationToken = null;
                    }

                    uow.SaveChanges();

                    uow.AuthorizationTokenRepository.Remove(authToken);
                    uow.SaveChanges();
                }

        public void SetMainToken(string userId, Guid authTokenId)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var mainAuthToken = uow.AuthorizationTokenRepository
                    .FindTokenById(authTokenId.ToString());

                if (mainAuthToken == null)
                {
                    throw new ApplicationException("Unable to find specified Auth-Token.");
                }

                var siblingIds = uow.AuthorizationTokenRepository
                    .GetPublicDataQuery()
                    .Where(x => x.UserID == userId && x.TerminalID == mainAuthToken.TerminalID)
                    .Select(x => x.Id)
                    .ToList();

                foreach (var siblingId in siblingIds)
                {
                    var siblingAuthToken = uow.AuthorizationTokenRepository
                        .FindTokenById(siblingId.ToString());

                    siblingAuthToken.IsMain = false;
                }

                mainAuthToken.IsMain = true;

                uow.SaveChanges();
            }
        }
    }
}
