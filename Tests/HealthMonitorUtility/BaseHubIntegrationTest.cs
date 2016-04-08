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
using Data.Entities;
using Data.Interfaces;
using Data.States;

namespace HealthMonitor.Utility
{
    public abstract class BaseHubIntegrationTest : BaseIntegrationTest
    {
        HttpClient _httpClient;

        protected virtual string TestUserEmail
        {
            get { return "integration_test_runner@fr8.company"; }
        }

        protected virtual string TestUserPassword
        {
            get { return "fr8#s@lt!"; }
        }

        protected string TestEmail;
        protected string TestEmailName;

        public CredentialsDTO GetDocuSignCredentials()
        {
            //var creds = new CredentialsDTO()
            //{
            //    Username = "integration_test_runner@fr8.company",
            //    Password = "I6HmXEbCxN",
            //    IsDemoAccount = false
            //};

            var creds = new CredentialsDTO()
            {
                Username = "freight.testing@gmail.com",
                Password = "I6HmXEbCxN",
                IsDemoAccount = true
            };
            return creds;
        }

        public BaseHubIntegrationTest()
        {
            ObjectFactory.Initialize();
            ObjectFactory.Configure(Hub.StructureMap.StructureMapBootStrapper.LiveConfiguration);

            // Use a common HttpClient for all REST operations within testing session 
            // to ensure the presense of the authentication cookie. 
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = GetHubBaseUrl();
            _httpClient.Timeout = TimeSpan.FromMinutes(2);

            Crate = new CrateManager();
            _hmacService = new Fr8HMACService();
            _baseUrl = GetHubApiBaseUrl();
            RestfulServiceClient = new RestfulServiceClient(_httpClient);

            // Get auth cookie from the Hub and save it to HttpClient's internal cookie storage
            LoginUser(TestUserEmail, TestUserPassword).Wait();

            // Initailize EmailAssert utility.
            TestEmail = ConfigurationManager.AppSettings["TestEmail"];
            TestEmailName = ConfigurationManager.AppSettings["TestEmail_Name"];
            string hostname = ConfigurationManager.AppSettings["TestEmail_Pop3Server"];
            int port = int.Parse(ConfigurationManager.AppSettings["TestEmail_Port"]);
            bool useSsl = ConfigurationManager.AppSettings["TestEmail_UseSsl"] == "true" ? true : false;
            string username = ConfigurationManager.AppSettings["TestEmail_Username"];
            string password = ConfigurationManager.AppSettings["TestEmail_Password"];
            EmailAssert.InitEmailAssert(TestEmail, hostname, port, useSsl, username, password);
        }

        public string GetHubApiBaseUrl()
        {
            return ConfigurationManager.AppSettings["HubApiBaseUrl"];
        }

        protected async Task LoginUser(string email, string password)
        {
            // The functions below re using ASP.NET MVC endpoi9nts to authenticate the user. 
            // Since we cannot use them in the self-hosted mode, we use WebAPI based 
            // authentication instead. 

            // Get login page and extract request validation token
            //var antiFogeryToken = await GetVerificationToken(_httpClient);

            // Login user
            //await Authenticate(email, password, antiFogeryToken, _httpClient);
            try
            {
                await AuthenticateWebApi(email, password);
            }
            catch (Exception ex)
            {
            }
        }

        protected Task RevokeTokens()
        {
            return RevokeTokens(TerminalName);
        }

        protected async Task RevokeTokens(string terminalName)
        {
            var tokens = await HttpGetAsync<IEnumerable<ManageAuthToken_Terminal>>(
                _baseUrl + "manageauthtoken/"
            );

            if (tokens != null)
            {
                var docusignTokens = tokens.FirstOrDefault(x => x.Name == terminalName);
                if (docusignTokens != null)
                {
                    foreach (var token in docusignTokens.AuthTokens)
                    {
                        await HttpPostAsync<string>(
                            _baseUrl + "manageauthtoken/revoke?id=" + token.Id.ToString(),
                            null
                        );
                    }
                }
            }
        }

        protected Uri GetHubBaseUrl()
        {
            var hubApiBaseUrl = new Uri(GetHubApiBaseUrl());
            var hubBaseUrl = new Uri(hubApiBaseUrl.Scheme + "://" + hubApiBaseUrl.Host + ":" + hubApiBaseUrl.Port);
            return hubBaseUrl;
        }

        private async Task AuthenticateWebApi(string email, string password)
        {
            await HttpPostAsync<string, object>(_baseUrl
                + string.Format("authentication/login?username={0}&password={1}", Uri.EscapeDataString(email), Uri.EscapeDataString(password)), null);
        }

        public async Task<List<CrateDescriptionDTO>> GetRuntimeCrateDescriptionsFromUpstreamActivities(Guid curActivityId)
        {
            var url = $"{GetHubApiBaseUrl()}/plannodes/upstream_actions/?id={curActivityId}";
            var upstreamActivities = await HttpGetAsync<List<ActivityDTO>>(url);
            var result = new List<CrateDescriptionDTO>();
            foreach (var activity in upstreamActivities)
            {
                var storage = Crate.FromDto(activity.CrateStorage);
                result.AddRange(storage.CratesOfType<CrateDescriptionCM>().SelectMany(x => x.Content.CrateDescriptions));
            }
            return result;
        }


        private async Task Authenticate(string email, string password, string verificationToken, HttpClient httpClient)
        {
            var authenticationEndpointUrl = "/dockyardaccount/login";

            var formContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("__RequestVerificationToken", verificationToken),
                    new KeyValuePair<string, string>("Email", email),
                    new KeyValuePair<string, string>("Password", password),
                });

            var response = await httpClient.PostAsync(authenticationEndpointUrl, formContent);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
        }

        private async Task<string> GetVerificationToken(HttpClient httpClient)
        {
            var loginFormUrl = "/dockyardaccount";
            var response = await httpClient.GetAsync(loginFormUrl);
            response.EnsureSuccessStatusCode();
            var loginPageText = await response.Content.ReadAsStringAsync();
            var regEx = new System.Text.RegularExpressions.Regex(@"<input\s+name=""__RequestVerificationToken""\s+type=""hidden""\s+value=\""([\w\d-_]+)""\s+\/>");
            var matches = regEx.Match(loginPageText);
            if (matches == null || matches.Groups.Count < 2)
            {
                throw new Exception("Unable to find verification token in the login page HTML code.");
            }
            string formToken = matches.Groups[1].Value;
            return formToken;
        }

        protected async Task<ActivityDTO> ConfigureActivity(ActivityDTO activity)
        {
            activity = await HttpPostAsync<ActivityDTO, ActivityDTO>(
                _baseUrl + "activities/configure",
                activity
            );

            return activity;
        }

        protected async Task<PayloadDTO> ExtractContainerPayload(ContainerDTO container)
        {
            var payload = await HttpGetAsync<PayloadDTO>(
                _baseUrl + "containers/payload?id=" + container.Id.ToString()
            );

            return payload;
        }

        protected async Task<ContainerDTO> ExecutePlan(PlanFullDTO plan)
        {
            var container = await HttpPostAsync<string, ContainerDTO>(
                _baseUrl + "plans/run?planId=" + plan.Id.ToString(),
                null
            );

            return container;
        }

        protected async Task SaveActivity(ActivityDTO activity)
        {
            await HttpPostAsync<ActivityDTO, ActivityDTO>(
                _baseUrl + "activities/save",
                activity
            );
        }

        public string ParseConditionToText(List<FilterConditionDTO> filterData)
        {
            var parsedConditions = new List<string>();

            filterData.ForEach(condition =>
            {
                string parsedCondition = condition.Field;

                switch (condition.Operator)
                {
                    case "eq":
                        parsedCondition += " = ";
                        break;
                    case "neq":
                        parsedCondition += " != ";
                        break;
                    case "gt":
                        parsedCondition += " > ";
                        break;
                    case "gte":
                        parsedCondition += " >= ";
                        break;
                    case "lt":
                        parsedCondition += " < ";
                        break;
                    case "lte":
                        parsedCondition += " <= ";
                        break;
                    default:
                        throw new NotSupportedException(string.Format("Not supported operator: {0}", condition.Operator));
                }

                parsedCondition += string.Format("'{0}'", condition.Value);
                parsedConditions.Add(parsedCondition);
            });

            var finalCondition = string.Join(" AND ", parsedConditions);

            return finalCondition;
        }
    }
}
