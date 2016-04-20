﻿using System;
using System.Collections.Generic;
using Data.Entities;
using Data.Interfaces;
using Data.Repositories;
using Data.States;
using Hub.StructureMap;
using NUnit.Framework;
using StructureMap;
using UtilitiesTesting;

namespace HubTests.Repositories
{
    internal class AuthorizationRepTestSupportService
    {
        public readonly Dictionary<Guid, string> Tokens = new Dictionary<Guid, string>();
        public readonly Dictionary<Guid, string> AddedTokens = new Dictionary<Guid, string>();
        public readonly Dictionary<Guid, string> UpdatedTokens = new Dictionary<Guid, string>();
        public readonly HashSet<Guid> DeletedTokens = new HashSet<Guid>();
        public readonly HashSet<Guid> QueriedTokens = new HashSet<Guid>();
        
        public void Reset()
        {
            Tokens.Clear();
            AddedTokens.Clear();
            UpdatedTokens.Clear();
            DeletedTokens.Clear();
            QueriedTokens.Clear();
        }
    }

    internal class AuthorizationRepositorySecurePartTester : AuthorizationTokenRepositoryBase
    {
        private readonly AuthorizationRepTestSupportService _testSupportService;

        public AuthorizationRepositorySecurePartTester(IUnitOfWork uow, AuthorizationRepTestSupportService testSupportService)
            : base(uow)
        {
            _testSupportService = testSupportService;
        }

        protected override void ProcessChanges(IEnumerable<AuthorizationTokenDO> adds, IEnumerable<AuthorizationTokenDO> updates, IEnumerable<AuthorizationTokenDO> deletes)
        {
            foreach (var authorizationTokenDo in adds)
            {
                _testSupportService.AddedTokens[authorizationTokenDo.Id] = authorizationTokenDo.Token;
            }

            foreach (var authorizationTokenDo in updates)
            {
                _testSupportService.UpdatedTokens[authorizationTokenDo.Id] = authorizationTokenDo.Token;
            }

            foreach (var authorizationTokenDo in deletes)
            {
                _testSupportService.DeletedTokens.Add(authorizationTokenDo.Id);
            }
        }

        protected override string QuerySecurePart(Guid id)
        {
            string result = null;

            _testSupportService.QueriedTokens.Add(id);

            _testSupportService.Tokens.TryGetValue(id, out result);

            return result;
        }
    }

    [TestFixture]
    [Category("AuthorizationTokenRepository")]
    public class AuthorizationTokenRepositoryTests : BaseTest
    {
        [SetUp]
        public void Setup()
        {
            StructureMapBootStrapper.ConfigureDependencies(StructureMapBootStrapper.DependencyType.TEST);

            ObjectFactory.Configure(cfg => cfg.For<AuthorizationRepTestSupportService>().Use(new AuthorizationRepTestSupportService()).Singleton());
            ObjectFactory.Configure(cfg => cfg.For<IAuthorizationTokenRepository>().Use<AuthorizationRepositorySecurePartTester>());

            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                uow.TerminalRepository.Add(new TerminalDO
                {
                    TerminalStatus = TerminalStatus.Active,
                    Id = 1,
                    Version = "v1",
                    Name = "Test terminal",
                    Secret = Guid.NewGuid().ToString()
                });

                uow.SaveChanges();
            }

            ObjectFactory.GetInstance<AuthorizationRepTestSupportService>().Reset();
        }

        private void SetupTester(Dictionary<Guid, string> tokens)
        {
            foreach (var token in tokens)
            {
                ObjectFactory.GetInstance<AuthorizationRepTestSupportService>().Tokens.Add(token.Key, token.Value);
            }
        }

        private AuthorizationTokenDO NewToken(IUnitOfWork uow, Guid id, string securePart)
        {
            AuthorizationTokenDO token = new AuthorizationTokenDO
            {
                Id = id,
                Token = securePart,
                TerminalID  = 1,
            };

            uow.AuthorizationTokenRepository.Add(token);

            return token;
        }
        
        [Test]
        public void CanAdd()
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var t1 = NewToken(uow, new Guid("{5F2E94FD-0592-45AD-82DD-34699FD10E69}"), "t1");
                var t2 = NewToken(uow, new Guid("{35B123A2-E8D9-49B2-A52E-AD8E5449668B}"), "t2");

                var tester = ObjectFactory.GetInstance<AuthorizationRepTestSupportService>();

                uow.SaveChanges();
                
                Assert.AreEqual(2, tester.AddedTokens.Count);
                Assert.AreEqual(0, tester.UpdatedTokens.Count);
                Assert.AreEqual(0, tester.DeletedTokens.Count);
                Assert.AreEqual(0, tester.QueriedTokens.Count);

                Assert.IsTrue(tester.AddedTokens.ContainsKey(t1.Id));
                Assert.IsTrue(tester.AddedTokens.ContainsKey(t2.Id));
            }
        }

        [Test]
        public void CanQueryById()
        {
            var tester = ObjectFactory.GetInstance<AuthorizationRepTestSupportService>();
            AuthorizationTokenDO t1;

            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                t1 = NewToken(uow, new Guid("{5F2E94FD-0592-45AD-82DD-34699FD10E69}"), "t1");
                NewToken(uow, new Guid("{35B123A2-E8D9-49B2-A52E-AD8E5449668B}"), "t2");

                uow.SaveChanges();
            }
            
            tester.Reset();

            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                

                var tFound = uow.AuthorizationTokenRepository.FindTokenById(t1.Id.ToString());
                
                Assert.AreEqual(0, tester.AddedTokens.Count);
                Assert.AreEqual(0, tester.UpdatedTokens.Count);
                Assert.AreEqual(0, tester.DeletedTokens.Count);
                Assert.AreEqual(1, tester.QueriedTokens.Count);

                Assert.AreEqual(t1.Id, tFound.Id);
                Assert.AreEqual(t1.Token, tFound.Token);
            }
        }

        [Test]
        public void CanQueryByExternalAccount()
        {
            var tester = ObjectFactory.GetInstance<AuthorizationRepTestSupportService>();
            AuthorizationTokenDO t1;

            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                t1 = NewToken(uow, new Guid("{5F2E94FD-0592-45AD-82DD-34699FD10E69}"), "t1");
                var t2 =  NewToken(uow, new Guid("{35B123A2-E8D9-49B2-A52E-AD8E5449668B}"), "t2");

                t1.ExternalAccountId = "ext1";
                t2.ExternalAccountId = "ext2";

                uow.SaveChanges();
            }

            tester.Reset();

            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var tFound = uow.AuthorizationTokenRepository
                    .FindTokenByExternalAccount("ext1", t1.TerminalID, t1.UserID);

                Assert.AreEqual(0, tester.AddedTokens.Count);
                Assert.AreEqual(0, tester.UpdatedTokens.Count);
                Assert.AreEqual(0, tester.DeletedTokens.Count);
                Assert.AreEqual(1, tester.QueriedTokens.Count);

                Assert.AreEqual(t1.Id, tFound.Id);
                Assert.AreEqual(t1.Token, tFound.Token);
            }
        }

        [Test]
        public void CanQueryByUserAndTerminal()
        {
            var tester = ObjectFactory.GetInstance<AuthorizationRepTestSupportService>();
            AuthorizationTokenDO t1;

            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                t1 = NewToken(uow, new Guid("{5F2E94FD-0592-45AD-82DD-34699FD10E69}"), "t1");

                t1.UserDO = new Fr8AccountDO(new EmailAddressDO("mail@mail.com"))
                {
                    UserName = "user1",
                    Id = "user1"
                };

                var t2 = NewToken(uow, new Guid("{35B123A2-E8D9-49B2-A52E-AD8E5449668B}"), "t2");

                t2.UserDO = new Fr8AccountDO(new EmailAddressDO("mail2@mail.com"))
                {
                    UserName = "user2",
                    Id = "user2"
                };

                uow.SaveChanges();

                tester.Reset();
            }

            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var tFound = uow.AuthorizationTokenRepository.FindToken("user1", 1, null);

                Assert.AreEqual(0, tester.AddedTokens.Count);
                Assert.AreEqual(0, tester.UpdatedTokens.Count);
                Assert.AreEqual(0, tester.DeletedTokens.Count);
                Assert.AreEqual(1, tester.QueriedTokens.Count);

                Assert.AreEqual(t1.Id, tFound.Id);
                Assert.AreEqual(t1.Token, tFound.Token);
            }
        }

        [Test]
        public void CanDelete()
        {
            var tester = ObjectFactory.GetInstance<AuthorizationRepTestSupportService>();

            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var t1 = NewToken(uow, new Guid("{5F2E94FD-0592-45AD-82DD-34699FD10E69}"), "t1");
                var t2 = NewToken(uow, new Guid("{35B123A2-E8D9-49B2-A52E-AD8E5449668B}"), "t2");
                
                uow.SaveChanges();

                tester.Reset();
                
                uow.AuthorizationTokenRepository.Remove(t1);

                uow.SaveChanges();

                Assert.AreEqual(0, tester.AddedTokens.Count);
                Assert.AreEqual(0, tester.UpdatedTokens.Count);
                Assert.AreEqual(1, tester.DeletedTokens.Count);
                Assert.AreEqual(0, tester.QueriedTokens.Count);

                Assert.IsTrue(tester.DeletedTokens.Contains(t1.Id));
                Assert.IsFalse(tester.DeletedTokens.Contains(t2.Id));
            }
        }

        [Test]
        public void CanUpdate()
        {
            var tester = ObjectFactory.GetInstance<AuthorizationRepTestSupportService>();

            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var t1 = NewToken(uow, new Guid("{5F2E94FD-0592-45AD-82DD-34699FD10E69}"), "t1");
                NewToken(uow, new Guid("{35B123A2-E8D9-49B2-A52E-AD8E5449668B}"), "t2");

                uow.SaveChanges();

                tester.Reset();

                t1.Token = "t3";

                uow.SaveChanges();

                Assert.AreEqual(0, tester.AddedTokens.Count);
                Assert.AreEqual(1, tester.UpdatedTokens.Count);
                Assert.AreEqual(0, tester.DeletedTokens.Count);
                Assert.AreEqual(0, tester.QueriedTokens.Count);

                Assert.AreEqual("t3", tester.UpdatedTokens[t1.Id]);
            }
        }


        [Test]
        public void CommitNoEditsWithoutSaveChanges()
        {
            var tester = ObjectFactory.GetInstance<AuthorizationRepTestSupportService>();

            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var t3 = NewToken(uow, new Guid("{35B123A2-E8D9-49B2-A52E-AD8E5449668B}"), "t3");

                uow.SaveChanges();
                
                tester.Reset();

                var t1 = NewToken(uow, new Guid("{5F2E94FD-0592-45AD-82DD-34699FD10E69}"), "t1");

                t1.Token = "t4";
                t3.Token = "dfgssw34et3";

                uow.AuthorizationTokenRepository.Remove(t3);

                Assert.AreEqual(0, tester.AddedTokens.Count);
                Assert.AreEqual(0, tester.UpdatedTokens.Count);
                Assert.AreEqual(0, tester.DeletedTokens.Count);
                Assert.AreEqual(0, tester.QueriedTokens.Count);
            }
        }
    }
}