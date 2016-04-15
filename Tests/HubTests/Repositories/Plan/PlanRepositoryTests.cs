﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Data.Entities;
using Data.Interfaces;
using Data.Repositories.Plan;
using Data.States;
using DayPilot.Web.Mvc.Events.Navigator;
using Hub.StructureMap;
using NUnit.Framework;
using StructureMap;
using UtilitiesTesting;

namespace HubTests.Repositories.Plan
{

    class PlanStorageProviderMonitor : IPlanStorageProvider
    {
        private PlanNodeDO _planNode;
        public PlanSnapshot.Changes SubmittedChanges;

        public PlanStorageProviderMonitor(PlanNodeDO planNode)
        {
            PlanTreeHelper.Visit(planNode, (x, y) =>
            {
                x.ParentPlanNodeId = y != null ? y.Id : (Guid?)null;
                x.ParentPlanNode = y;
            });

            _planNode = planNode;
        }

        public PlanNodeDO LoadPlan(Guid planMemberId)
        {
            return _planNode;
        }

        public void Update(PlanSnapshot.Changes changes)
        {
            SubmittedChanges = changes;
        }

        public IQueryable<PlanDO> GetPlanQuery()
        {
            throw new NotImplementedException();
        }

        public IQueryable<ActivityDO> GetActivityQuery()
        {
            throw new NotImplementedException();
        }

        public IQueryable<PlanNodeDO> GetNodesQuery()
        {
            throw new NotImplementedException();
        }
    }

    internal class ExpectedChanges
    {
        public readonly List<Guid> Deleted = new List<Guid>();
        public readonly List<Guid> Inserted = new List<Guid>();
        public readonly List<ExpectedObjectChange> Updates = new List<ExpectedObjectChange>();
    }

    internal class ExpectedObjectChange
    {
        public readonly Guid ObjectId;
        public readonly String PropertyName;
        public readonly object Value;

        public ExpectedObjectChange(Guid objectId, string propertyName, object value)
        {
            ObjectId = objectId;
            PropertyName = propertyName;
            Value = value;
        }
    }

    [TestFixture]
    [Category("PlanRepositoryTests")]
    public class PlanRepositoryTests : BaseTest
    {
        [SetUp]
        public void Setup()
        {
            base.SetUp();
            StructureMapBootStrapper.ConfigureDependencies(StructureMapBootStrapper.DependencyType.TEST);
        }

        private static Guid NewGuid(int id)
        {
            return new Guid(id, (short)0, (short)0, (byte)0, (byte)0, (byte)0, (byte)0, (byte)0, (byte)0, (byte)0, (byte)0);
        }

        private PlanNodeDO GenerateRefTree()
        {
            return new ActivityDO
            {
                Id = NewGuid(1),
                Label = "Base1",
                ChildNodes =
                {
                    new ActivityDO
                    {
                        Id = NewGuid(2),
                        Label = "Base2",
                    },
                    new ActivityDO()
                    {
                        Id = NewGuid(3),
                        Label = "Base3",
                    }
                }
            };
        }

        private PlanDO GenerateTestPlan()
        {
            return new PlanDO
            {
                Id = NewGuid(13),
                Name = "Plan",
                PlanState = PlanState.Active,
                Description = "PlanDesc",
                Fr8Account = new Fr8AccountDO()
                {
                    Id = "acoountId",
                    FirstName = "Account"
                },
                Fr8AccountId = "acoountId",
                ChildNodes =
                {
                    new ActivityDO
                    {
                        RootPlanNodeId = NewGuid(13),
                        Id = NewGuid(1),
                        ActivityTemplate = new ActivityTemplateDO
                        {
                            TerminalId = 1,
                            Id = FixtureData.GetTestGuidById(1),
                            Name = "New template",
                        },
                        ActivityTemplateId = FixtureData.GetTestGuidById(1),
                        AuthorizationToken = new AuthorizationTokenDO
                        {
                             TerminalID = 1,
                            Id = NewGuid(34),
                        },
                        AuthorizationTokenId = NewGuid(34),
                        Label = "label",
                        CrateStorage = "crates",
                        Ordering = 6666,
                        Fr8Account = new Fr8AccountDO()
                        {
                            Id = "acoountId",
                            FirstName = "Account"
                        },
                        Fr8AccountId = "acoountId",
                        ChildNodes =
                        {
                            new ActivityDO
                            {
                                ActivityTemplate = new ActivityTemplateDO
                                {
                                    TerminalId = 1,
                                    Id = FixtureData.GetTestGuidById(1),
                                    Name = "New template",
                                },
                                ActivityTemplateId = FixtureData.GetTestGuidById(1),
                                RootPlanNodeId = NewGuid(13),
                                Id = NewGuid(2),
                                Fr8Account = new Fr8AccountDO()
                                {
                                    Id = "acoountId",
                                    FirstName = "Account"
                                },
                                Fr8AccountId = "acoountId",
                            },
                            new ActivityDO
                            {
                                ActivityTemplate = new ActivityTemplateDO
                                {
                                    TerminalId = 1,
                                    Id = FixtureData.GetTestGuidById(1),
                                    Name = "New template",
                                },
                                ActivityTemplateId = FixtureData.GetTestGuidById(1),
                                RootPlanNodeId = NewGuid(13),
                                Id = NewGuid(3),
                                Fr8Account = new Fr8AccountDO()
                                {
                                    Id = "acoountId",
                                    FirstName = "Account"
                                },
                                Fr8AccountId = "acoountId",
                            }
                        }
                    }
                }
            };
        }

        private static bool AreEquals(PlanNodeDO a, PlanNodeDO b)
        {
            var snapShotA = new PlanSnapshot(a, false);
            var snapShotB = new PlanSnapshot(b, false);

            return !snapShotB.Compare(snapShotA).HasChanges;
        }

        private void ValidateChanges(List<Guid> expected, List<PlanNodeDO> actual)
        {
            Assert.AreEqual(expected.Count, actual.Count);
            foreach (var id in expected)
            {
                var localId = id;
                Assert.IsTrue(actual.Any(x => x.Id == localId));
            }
        }

        private void ValidateChanges(ExpectedChanges expectedChanges, PlanSnapshot.Changes actualChanges)
        {
            ValidateChanges(expectedChanges.Inserted, actualChanges.Insert);
            ValidateChanges(expectedChanges.Deleted, actualChanges.Delete);

            Assert.AreEqual(expectedChanges.Updates.Count, actualChanges.Update.Count);

            foreach (var update in expectedChanges.Updates.GroupBy(x => x.ObjectId))
            {
                var updateId = update.Key;
                var expectedUpdate = update.ToArray();
                var actualUpdate = actualChanges.Update.FirstOrDefault(x => x.Node.Id == updateId);

                Assert.NotNull(actualUpdate);
                Assert.AreEqual(expectedUpdate.Length, actualUpdate.ChangedProperties.Count);

                foreach (var propUpdate in expectedUpdate)
                {
                    var name = propUpdate.PropertyName;
                    var actualProp = actualUpdate.ChangedProperties.FirstOrDefault(x => x.Name == name);

                    Assert.NotNull(actualProp);
                    Assert.AreEqual(propUpdate.Value, actualProp.GetValue(actualUpdate.Node));
                }
            }
        }

        [Test]
        public void CanGetById()
        {
            var provider = new PlanStorageProviderMonitor(GenerateRefTree());
            var repository = new PlanRepository(new PlanStorage(new PlanCache(new ExpirationStrategyMock()), provider));

            Assert.AreEqual(NewGuid(2), repository.GetById<ActivityDO>(NewGuid(2)).Id);
        }

        [Test]
        public void CanUpdateProperties()
        {
            var provider = new PlanStorageProviderMonitor(GenerateRefTree());
            var repository = new PlanRepository(new PlanStorage(new PlanCache(new ExpirationStrategyMock()), provider));
            var expectedChanges = new ExpectedChanges
            {
                Updates =
                {
                    new ExpectedObjectChange(NewGuid(2), "Label", "newName"),
                    new ExpectedObjectChange(NewGuid(3), "Label", "newName3")
                }

            };

            repository.GetById<ActivityDO>(NewGuid(2)).Label = "newName";
            repository.GetById<ActivityDO>(NewGuid(3)).Label = "newName3";

            repository.SaveChanges();

            ValidateChanges(expectedChanges, provider.SubmittedChanges);
        }

        [Test]
        public void CanUpdateStructure()
        {
            var provider = new PlanStorageProviderMonitor(GenerateRefTree());
            var repository = new PlanRepository(new PlanStorage(new PlanCache(new ExpirationStrategyMock()), provider));
            var expectedChanges = new ExpectedChanges
            {
                Inserted = { NewGuid(4) },
                Deleted = { NewGuid(3) }
            };

            repository.GetById<ActivityDO>(NewGuid(3)).Label = "newName";
            repository.GetById<ActivityDO>(NewGuid(3)).RemoveFromParent();
            repository.GetById<ActivityDO>(NewGuid(1)).ChildNodes.Add(new ActivityDO
            {
                Id = NewGuid(4),
                Label = "Base4",
            });

            repository.SaveChanges();

            ValidateChanges(expectedChanges, provider.SubmittedChanges);
        }

        [Test]
        public void CanAddPlan()
        {
            var provider = new PersistentPlanStorage(null);
            var cache = new PlanCache(new ExpirationStrategyMock());
            var repository = new PlanRepository(new PlanStorage(cache, provider));
            var plan = GenerateTestPlan();
            repository.Add(plan);

            repository.SaveChanges();

            var loadedPlan = provider.LoadPlan(Guid.Empty);

            Assert.IsTrue(AreEquals(plan, loadedPlan));
            Assert.IsTrue(AreEquals(repository.GetById<PlanDO>(NewGuid(13)), plan));
        }


        [Test]
        public void CanAddPlanWithEmptyDefaultIds()
        {
            var provider = new PersistentPlanStorage(null);
            var cache = new PlanCache(new ExpirationStrategyMock());
            var repository = new PlanRepository(new PlanStorage(cache, provider));

            var refPlan = GenerateTestPlan();
            PlanTreeHelper.Visit(refPlan, x => x.Id = Guid.Empty);

            repository.Add(refPlan);
            repository.SaveChanges();

            PlanTreeHelper.Visit(refPlan, x => Assert.IsTrue(x.Id != Guid.Empty));

            var loadedPlan = provider.LoadPlan(Guid.Empty);

            Assert.IsTrue(AreEquals(refPlan, loadedPlan));
        }

        [Test]
        public void CanAddPlanInEF()
        {
            var plan = GenerateTestPlan();
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                uow.ActivityTemplateRepository.Add(new ActivityTemplateDO
                {
                    TerminalId = 1,

                    Id = FixtureData.GetTestGuidById(1),
                    Name = "New template",
                });

                uow.UserRepository.Add(new Fr8AccountDO()
                {
                    Id = "acoountId",
                    FirstName = "Account"
                });

                uow.TerminalRepository.Add(new TerminalDO()
                {
                    Id = 1,
                    TerminalStatus = TerminalStatus.Active,
                    Name = "terminal",
                    Version = "1"

                });

                uow.AuthorizationTokenRepository.Add(new AuthorizationTokenDO
                {
                    TerminalID = 1,
                    Id = NewGuid(34),
                });

                uow.TerminalRepository.Add(new TerminalDO
                {
                    Name = "asdfasdf",
                    Version = "1",
                    Id = 1,
                    TerminalStatus = 1
                });
                uow.PlanRepository.Add(plan);
                uow.SaveChanges();
            }

            var container = ObjectFactory.Container.CreateChildContainer();

            container.Configure(x => x.For<IPlanCache>().Use<PlanCache>().Singleton());

            using (var uow = container.GetInstance<IUnitOfWork>())
            {
                var loadedPlan = uow.PlanRepository.GetById<PlanDO>(NewGuid(13));
                Assert.IsTrue(AreEquals(plan, loadedPlan));
            }
        }

        [Test]
        public void CanPersistPropertyChanges()
        {
            var provider = new PlanStorageProviderMonitor(GenerateRefTree());
            var cache = new PlanCache(new ExpirationStrategyMock());
            var repository = new PlanRepository(new PlanStorage(cache, provider));

            repository.GetById<ActivityDO>(NewGuid(2)).Label = "newName";
            repository.GetById<ActivityDO>(NewGuid(3)).Label = "newName3";

            repository.SaveChanges();

            repository = new PlanRepository(new PlanStorage(cache, provider));

            Assert.AreEqual("newName", repository.GetById<ActivityDO>(NewGuid(2)).Label);
            Assert.AreEqual("newName3", repository.GetById<ActivityDO>(NewGuid(3)).Label);
        }

    }
}
