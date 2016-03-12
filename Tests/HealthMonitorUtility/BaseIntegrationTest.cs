﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;
using Hub.Interfaces;
using Hub.Security;
using Newtonsoft.Json;
using Data.Crates;
using Data.Interfaces.DataTransferObjects;
using Data.Interfaces.Manifests;
using Hub.Managers;
using Hub.Managers.APIManagers.Transmitters.Restful;
using System.Linq;
using NUnit.Framework;
using Data.Constants;
using Data.Interfaces.DataTransferObjects.Helpers;
using StructureMap;
using System.Net.Http;
using System.Net;
using System.Linq;
using Data.Interfaces;

namespace HealthMonitor.Utility
{
    public abstract class BaseIntegrationTest
    {
        public ICrateManager Crate { get; set; }
        public IRestfulServiceClient RestfulServiceClient { get; set; }
        public IHMACService _hmacService { get; set; }
        private string _terminalSecret;
        private string _terminalId;
        HttpClient _httpClient;
        protected string _baseUrl;
        protected int currentTerminalVersion = 1;

        protected string TerminalSecret
        {
            get
            {
                return _terminalSecret ?? (_terminalSecret = ConfigurationManager.AppSettings[TerminalName + "TerminalSecret"]);
            }
        }
        protected string TerminalId
        {
            get
            {
                return _terminalId ?? (_terminalId = ConfigurationManager.AppSettings[TerminalName + "TerminalId"]);
            }
        }
        protected string TerminalUrl
        {
            get
            {
                return _terminalUrl ?? (_terminalUrl = GetTerminalUrl());
            }
        }

        protected string _terminalUrl;

        public BaseIntegrationTest()
        {
            RestfulServiceClient = new RestfulServiceClient();
            Crate = new CrateManager();
        }

        private string GetTerminalUrl()
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var terminal = uow.TerminalRepository.GetQuery()
                    .FirstOrDefault(t => t.Version == currentTerminalVersion.ToString() && t.Name == TerminalName);
                if (null == terminal)
                {
                    throw new Exception(
                        String.Format("Terminal with name {0} and version {1} not found", TerminalName, currentTerminalVersion));
                }
                if (terminal.Endpoint.StartsWith("http://"))
                {
                    return terminal.Endpoint;
                }
                else if (terminal.Endpoint.StartsWith("https://"))
                {
                    return terminal.Endpoint.Replace("https://", "http://");
                }
                else
                {
                    return "http://" + terminal.Endpoint;
                }
            }
        }

        public abstract string TerminalName { get; }


        public string GetTerminalDiscoverUrl()
        {
            return TerminalUrl + "/terminals/discover";
        }

        public string GetTerminalConfigureUrl()
        {
            return TerminalUrl + "/activities/configure";
        }

        public string GetTerminalActivateUrl()
        {
            return TerminalUrl + "/activities/activate";
        }

        public string GetTerminalDeactivateUrl()
        {
            return TerminalUrl + "/activities/deactivate";
        }

        public string GetTerminalRunUrl()
        {
            return TerminalUrl + "/activities/run";
        }

        public void CheckIfPayloadHasNeedsAuthenticationError(PayloadDTO payload)
        {
            var storage = Crate.GetStorage(payload);
            var operationalStateCM = storage.CrateContentsOfType<OperationalStateCM>().Single();

            //extract current error message from current activity response
            ErrorDTO errorMessage;
            operationalStateCM.CurrentActivityResponse.TryParseErrorDTO(out errorMessage);

            Assert.AreEqual(ActivityResponse.Error.ToString(), operationalStateCM.CurrentActivityResponse.Type);
            Assert.AreEqual(ActivityErrorCode.NO_AUTH_TOKEN_PROVIDED, operationalStateCM.CurrentActivityErrorCode);
            Assert.AreEqual("No AuthToken provided.", errorMessage.Message);
        }

        private async Task<Dictionary<string, string>> GetHMACHeader<T>(Uri requestUri, string userId, T content)
        {
            return await _hmacService.GenerateHMACHeader(requestUri, TerminalId, TerminalSecret, userId, content);
        }
        public async Task<TResponse> HttpPostAsync<TRequest, TResponse>(string url, TRequest request)
        {
            var uri = new Uri(url);
            return await RestfulServiceClient.PostAsync<TRequest, TResponse>(uri, request, null, null);
        }

        public async Task<TResponse> HttpPostAsync<TResponse>(string url, HttpContent content)
        {
            var uri = new Uri(url);
            return await RestfulServiceClient.PostAsync<TResponse>(uri, content, null, null);
        }
        public async Task HttpDeleteAsync(string url)
        {
            var uri = new Uri(url);
            await RestfulServiceClient.DeleteAsync(uri, null, null);
        }
        public async Task<TResponse> HttpGetAsync<TResponse>(string url)
        {
            var uri = new Uri(url);
            return await RestfulServiceClient.GetAsync<TResponse>(uri);
        }
    }
}
