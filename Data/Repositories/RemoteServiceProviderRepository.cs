﻿using System.Linq;
using Data.Entities;
using Data.Interfaces;
using Data.States;
using Newtonsoft.Json;
using Utilities;

namespace Data.Repositories
{
    public class RemoteServiceProviderRepository : GenericRepository<RemoteServiceProviderDO>, IRemoteCalendarProviderRepository
    {
        internal RemoteServiceProviderRepository(IUnitOfWork uow)
            : base(uow)
        {

        }

        public RemoteServiceProviderDO GetByName(string name)
        {
            return GetQuery().FirstOrDefault(rcp => rcp.Name == name);
        }

        public void CreateRemoteServiceProviders(IConfigRepository configRepository)
        {
            var clientID = configRepository.Get("GoogleClientId");
            var clientSecret = configRepository.Get("GoogleClientSecret");
            var providers = new[]
                {
                    new RemoteServiceProviderDO
                        {
                            Name = "Google",
                            AuthType = ServiceAuthorizationType.OAuth2,
                            AppCreds = JsonConvert.SerializeObject(
                                new
                                    {
                                        ClientId = clientID,
                                        ClientSecret = clientSecret,
                                        Scopes = "https://spreadsheets.google.com/feeds,https://docs.google.com/feeds"
                                    }),
                            EndPoint = ""
                        }
                };
            foreach (var provider in providers)
            {
                var existingRow = GetByName(provider.Name);
                if (existingRow == null)
                {
                    Add(provider);
                }
                else
                {
                    existingRow.AuthType = provider.AuthType;
                    existingRow.AppCreds = provider.AppCreds;
                    existingRow.EndPoint = provider.EndPoint;
                }
            }
        }
    }

    public interface IRemoteCalendarProviderRepository : IGenericRepository<RemoteServiceProviderDO>
    {
        RemoteServiceProviderDO GetByName(string name);
    }
}
