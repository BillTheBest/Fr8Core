﻿using System;
using Hub.Services;
using NUnit.Framework;
using Data.Entities;
using StructureMap;
using Data.Interfaces;
using Data.States;
using Data.Interfaces.DataTransferObjects;
using UtilitiesTesting;
using UtilitiesTesting.Fixtures;
using Owin;
using System.Web.Http;
using System.Web.Http.Dispatcher;
using System.Collections.Generic;
using Hub.Managers.APIManagers.Transmitters.Restful;
using Moq;
using Hub.Managers;
using Data.Interfaces.Manifests;
using Data.Crates;

namespace DockyardTest.Security
{
    [TestFixture]
    [Category("Authorization")]
    public class AuthorizationMethodsTests : BaseTest
    {
        private Authorization _authorization;

        private ICrateManager _crate;

        private readonly string Token = @"{""Email"":""64684b41-bdfd-4121-8f81-c825a6a03582"",""ApiPassword"":""HyCXOBeGl/Ted9zcMqd7YEKoN0Q=""}";

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _authorization = new Authorization();
            _crate = ObjectFactory.GetInstance<ICrateManager>();
        }   

        private TerminalDO CreateAndAddTerminalDO(int authType = AuthenticationType.None)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var terminalDO = new TerminalDO()
                {
                    Name = "terminalTest",
                    Version = "1",
                    TerminalStatus = 1,
                    Endpoint = "localhost:39504",
                    AuthenticationType = authType,
                    Secret = Guid.NewGuid().ToString()
                };

                uow.TerminalRepository.Add(terminalDO);
                uow.SaveChanges();

                return terminalDO;
            }
        }

        private Fr8AccountDO CreateAndAddUserDO()
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var user = new Fr8Account();
                var emailAddress = new EmailAddressDO
                {
                    Address = "tester@gmail.com",
                    Name = "Test Tester"
                };

                var userDO = uow.UserRepository.GetOrCreateUser(emailAddress);
                uow.SaveChanges();

                return userDO;
            }
        }

        private AuthorizationTokenDO CreateAndAddTokenDO()
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var terminalDO = CreateAndAddTerminalDO();
                var userDO = CreateAndAddUserDO();

                var tokenDO = new AuthorizationTokenDO()
                {
                    UserID = userDO.Id,
                    TerminalID = terminalDO.Id,
                    AuthorizationTokenState = AuthorizationTokenState.Active
                };
                uow.AuthorizationTokenRepository.Add(tokenDO);

                tokenDO.ExpiresAt = DateTime.UtcNow.AddYears(100);
                tokenDO.Token = Token;
                uow.SaveChanges();

                return tokenDO;
            }
        }

        [Test]
        public void CanGetTokenByUserIdAndTerminalId()
        {
            var tokenDO = CreateAndAddTokenDO();
            var testToken = _authorization.GetToken(tokenDO.UserDO.Id, tokenDO.TerminalID);

            Assert.AreEqual(Token, testToken);
            
        }

        [Test]
        public void GetTokenByUserIdAndTerminalIdIsNull()
        {
            var token = _authorization.GetToken("null", 0);
            Assert.IsNull(token);
        }

//        [Test]
//        public void CanGetTerminalToken()
//        {
//            var tokenDO = CreateAndAddTokenDO();
//            var testToken = _authorization.GetTerminalToken(tokenDO.TerminalID);
//
//            Assert.AreEqual(Token, testToken);            
//        }

        [Test]
        public void CanPrepareAuthToken()
        {
            var tokenDO = CreateAndAddTokenDO();
            tokenDO.Terminal.AuthenticationType = AuthenticationType.Internal;

            var activityDTO = new ActivityDTO();
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var activityTemplateDO = new ActivityTemplateDO(
                    "test_name",
                    "test_label",
                    "1",
                    "test_description",
                    tokenDO.TerminalID
                );
                activityTemplateDO.NeedsAuthentication = true;
                uow.ActivityTemplateRepository.Add(activityTemplateDO);
                uow.SaveChanges();

                var planDO = new PlanDO()
                {
                    Id = FixtureData.GetTestGuidById(23),
                    Description = "HealthDemo Integration Test",
                    Name = "StandardEventTesting",
                    RouteState = RouteState.Active,
                    Fr8Account = tokenDO.UserDO
                };
                uow.RouteRepository.Add(planDO);
                uow.SaveChanges();

                var activityDO = new ActivityDO()
                {
                    ParentRouteNode = planDO,
                    ParentRouteNodeId = planDO.Id,
                    Name = "testaction",

                    Id = FixtureData.GetTestGuidById(1),
                    ActivityTemplateId = activityTemplateDO.Id,
                    ActivityTemplate = activityTemplateDO,
                    AuthorizationTokenId = tokenDO.Id,
                    AuthorizationToken = tokenDO,
                    Ordering = 1
                };

                uow.ActivityRepository.Add(activityDO);
                uow.SaveChanges();

                activityDTO.Id = activityDO.Id;
                activityDTO.ActivityTemplateId = activityTemplateDO.Id;
            }
            
                
            _authorization.PrepareAuthToken(activityDTO);

            Assert.AreEqual(Token, activityDTO.AuthToken.Token);
        }

        [Test]
        public void CanAuthenticateInternal()
        {   
            var tokenDO = CreateAndAddTokenDO();
            var activityTemplateDO = new ActivityTemplateDO("test_name", "test_label", "1", "test_description", tokenDO.TerminalID);
            activityTemplateDO.Terminal = tokenDO.Terminal;
            activityTemplateDO.Terminal.AuthenticationType = AuthenticationType.Internal;

            var activityDO = FixtureData.TestActivity1();
            activityDO.ActivityTemplate = activityTemplateDO;
            activityDO.AuthorizationToken = tokenDO;

            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {   
                uow.ActivityTemplateRepository.Add(activityTemplateDO);
                uow.RouteNodeRepository.Add(activityDO);
                uow.SaveChanges();
            }

            var credentialsDTO = new CredentialsDTO()
            {
                Username = "Username",
                Password = "Password",
                Domain = "Domain"
            };

            var result = _authorization.AuthenticateInternal(
               tokenDO.UserDO,
               tokenDO.Terminal,
               credentialsDTO.Domain,
               credentialsDTO.Username,
               credentialsDTO.Password
            );

            //Assert
            Mock<IRestfulServiceClient> restClientMock = Mock.Get(
                ObjectFactory.GetInstance<IRestfulServiceClient>()
            );
                        
            //verify that the post call is made 
            restClientMock.Verify(
                client => client.PostAsync<CredentialsDTO>(
                new Uri("http://" + activityTemplateDO.Terminal.Endpoint + "/authentication/internal"),
                It.Is < CredentialsDTO >(it=> it.Username ==  credentialsDTO.Username && 
                                              it.Password == credentialsDTO.Password &&
                                              it.Domain == credentialsDTO.Domain), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()), Times.Exactly(1));
                       

            restClientMock.VerifyAll();
        }


        [Test]
        public void CanGetOAuthToken()
        {
            var terminalDO = CreateAndAddTerminalDO();

            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var activityTemplateDO = new ActivityTemplateDO("test_name", "test_label", "1", "test_description", terminalDO.Id);
                uow.ActivityTemplateRepository.Add(activityTemplateDO);
                uow.SaveChanges();
            }

            var externalAuthenticationDTO = new ExternalAuthenticationDTO()
            {
                RequestQueryString = "?id"
            };

            var result = _authorization.GetOAuthToken(terminalDO, externalAuthenticationDTO);

            //Assert
            Mock<IRestfulServiceClient> restClientMock = Mock.Get(ObjectFactory.GetInstance<IRestfulServiceClient>());

            //verify that the post call is made 
            restClientMock.Verify(
                client => client.PostAsync<ExternalAuthenticationDTO>(new Uri("http://" + terminalDO.Endpoint + "/authentication/token"),
                externalAuthenticationDTO, It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()), Times.Exactly(1));

            restClientMock.VerifyAll();


        }

        [Test]
        public void CanGetOAuthInitiationURL()
        {
            var tokenDO = CreateAndAddTokenDO();
            tokenDO.Terminal.AuthenticationType = AuthenticationType.Internal;

            var activityTemplateDO = new ActivityTemplateDO(
                "test_name", "test_label", "1", "test_description", tokenDO.TerminalID
            );

            var activityDO = FixtureData.TestActivity1();
            activityDO.ActivityTemplate = activityTemplateDO;
            activityDO.AuthorizationToken = tokenDO;

            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {   
                uow.ActivityTemplateRepository.Add(activityTemplateDO);
                uow.RouteNodeRepository.Add(activityDO);
                uow.SaveChanges();
            }

            var result = _authorization.GetOAuthInitiationURL(tokenDO.UserDO, tokenDO.Terminal);

            //Assert
            Mock<IRestfulServiceClient> restClientMock = Mock.Get(ObjectFactory.GetInstance<IRestfulServiceClient>());

            //verify that the post call is made 
            restClientMock.Verify(
                client => client.PostAsync(
                    new Uri("http://" + tokenDO.Terminal.Endpoint + "/authentication/initial_url"), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()
                ), 
                Times.Exactly(1)
            );

            restClientMock.VerifyAll();
        }

        [Test]
        public void ValidateAuthenticationNeededIsTrue()
        {
            var userDO = CreateAndAddUserDO();
            
            var terminalDO = CreateAndAddTerminalDO(AuthenticationType.Internal);

            var activityDO = FixtureData.TestActivity1();
            var activityDTO = new ActivityDTO();

            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var activityTemplateDO = new ActivityTemplateDO(
                    "test_name",
                    "test_label",
                    "1",
                    "test_description",
                    terminalDO.Id
                );

                activityTemplateDO.NeedsAuthentication = true;

                uow.ActivityTemplateRepository.Add(activityTemplateDO);
                activityDTO.ActivityTemplateId = activityTemplateDO.Id;

                activityDO.ActivityTemplate = activityTemplateDO;
                uow.ActivityRepository.Add(activityDO);

                uow.SaveChanges();

                activityDTO.ActivityTemplateId = activityTemplateDO.Id;
                activityDTO.Id = activityDO.Id;
            }
            var testResult = _authorization.ValidateAuthenticationNeeded(userDO.Id, activityDTO);

            Assert.IsTrue(testResult);
        }

        [Test]
        public void ValidateAuthenticationNeededIsFalse()
        {
            var tokenDO = CreateAndAddTokenDO();
            tokenDO.Terminal.AuthenticationType = AuthenticationType.Internal;

            var activityDO = FixtureData.TestActivity1();
            var activityDTO = new ActivityDTO();

            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var activityTemplateDO = new ActivityTemplateDO("test_name", "test_label", "1", "test_description", tokenDO.TerminalID);
                uow.ActivityTemplateRepository.Add(activityTemplateDO);
                activityDTO.ActivityTemplateId = activityTemplateDO.Id;

                activityDO.ActivityTemplate = activityTemplateDO;
                activityDO.AuthorizationToken = tokenDO;
                uow.ActivityRepository.Add(activityDO);

                uow.SaveChanges();

                activityDTO.Id = activityDO.Id;
                activityDTO.ActivityTemplateId = activityTemplateDO.Id;
            }

            var testResult = _authorization.ValidateAuthenticationNeeded(tokenDO.UserID, activityDTO);

            Assert.IsFalse(testResult);
        }

        [Test]
        public void TestAddAuthenticationCrate()
        {
            var userDO = CreateAndAddUserDO();
            var terminalDO = CreateAndAddTerminalDO();
            terminalDO.AuthenticationType = AuthenticationType.Internal;

            var activityDTO = new ActivityDTO();

            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var activityTemplateDO = new ActivityTemplateDO("test_name", "test_label", "1", "test_description", terminalDO.Id);
                uow.ActivityTemplateRepository.Add(activityTemplateDO);
                activityDTO.ActivityTemplateId = activityTemplateDO.Id;
                uow.SaveChanges();

                activityDTO.ActivityTemplateId = activityTemplateDO.Id;
            }

            
            _authorization.AddAuthenticationCrate(activityDTO, AuthenticationType.Internal);
            Assert.IsTrue(IsCratePresents(activityDTO, AuthenticationMode.InternalMode));

            _authorization.AddAuthenticationCrate(activityDTO, AuthenticationType.External);
            Assert.IsTrue(IsCratePresents(activityDTO, AuthenticationMode.ExternalMode));

            _authorization.AddAuthenticationCrate(activityDTO, AuthenticationType.InternalWithDomain);
            Assert.IsTrue(IsCratePresents(activityDTO, AuthenticationMode.InternalModeWithDomain));
        }

        private bool IsCratePresents(ActivityDTO activityDTO, AuthenticationMode mode)
        {
            var result = false;
            foreach (var crate in activityDTO.CrateStorage.Crates)
            {
                if ( (int)mode == crate.Contents["Mode"].ToObject<int>())
                {
                    result = true;
                    break;
                }
               
            }

            return result;
        }
    }
}
