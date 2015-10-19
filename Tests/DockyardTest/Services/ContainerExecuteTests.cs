﻿using System;
// This alias is used to avoid ambiguity between StructureMap.IContainer and Core.Interfaces.IContainer
using InternalInterface = Core.Interfaces;
using Core.Interfaces;
using Core.Services;
using Data.Entities;
using Data.Interfaces;
using Data.States;
using NUnit.Framework;
using StructureMap;
using UtilitiesTesting;
using UtilitiesTesting.Fixtures;
using Newtonsoft.Json;
using System.Collections.Generic;
using Data.Interfaces.DataTransferObjects;

namespace DockyardTest.Services
{
    
    [TestFixture]
    [Category("ContainerExecute")]
    public class ContainerExecuteTests: BaseTest
    {
        private InternalInterface.IContainer _container;

        [SetUp]
        //constructor method as it is run at the test start
        public override void SetUp()
        {
            base.SetUp();

            _container = ObjectFactory.GetInstance<InternalInterface.IContainer>();
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public async void Execute_ContainerDoIsNull_ThrowsArgumentNullException()
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                await _container.Execute(uow, null);
            }
        }
        
        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public async void Execute_ContainerDoCurrentActivityIsNull_ThrowsArgumentNullException()
        {
            //Get ProcessDO entity from static partial class FixtureData for already prepared data
            //The CurrentActivity value is already set to null and pass it immediately to service
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                await _container.Execute(uow, FixtureData.TestContainerCurrentActivityNULL());
            }
        }

// DO-1270
//        [Test]
//        public async void Execute_CurrentActivityStateIsActive_ExpectedException()
//        {
//            string crateStorage = GetCrateStorageAsString();
//
//            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
//            {
//                var processDO = FixtureData.TestProcessExecute();
//                var currAction = FixtureData.TestAction4();
//                currAction.CrateStorage = crateStorage;
//                var nextAction = FixtureData.TestAction5();
//                nextAction.CrateStorage = crateStorage;
//                processDO.CurrentActivity = currAction;
//                processDO.NextActivity = nextAction;
//
//                uow.ProcessRepository.Add(processDO);
//                uow.ActivityRepository.Add(currAction);
//                uow.ActivityRepository.Add(nextAction);
//
//                uow.SaveChanges();
//            }
//            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
//            {
//                var processDO = uow.ProcessRepository.GetByKey(49);
//                var ex = Assert.Throws<Exception>(async () => await _process.Execute(uow, processDO));
//                Assert.AreEqual("Action ID: 3 status is 4.", ex.Message);
//            }
//        }
//        [Test]
//        public async void Execute_CurrentActivityStateIsDeactive_ExpectedException()
//        {
//            string crateStorage = GetCrateStorageAsString();
//
//            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
//            {
//                var processDO = FixtureData.TestProcessExecute();
//                var currAction = FixtureData.TestAction4();
//                currAction.CrateStorage = crateStorage;
//                var nextAction = FixtureData.TestAction5();
//                nextAction.CrateStorage = crateStorage;
//                processDO.CurrentActivity = currAction;
//                processDO.NextActivity = nextAction;
//
//                uow.ProcessRepository.Add(processDO);
//                uow.ActivityRepository.Add(currAction);
//                uow.ActivityRepository.Add(nextAction);
//
//                uow.SaveChanges();
//            }
//            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
//            {
//                var processDO = uow.ProcessRepository.GetByKey(49);
//                var ex = Assert.Throws<Exception>(async () => await _process.Execute(uow, processDO));
//                Assert.AreEqual("Action ID: 3 status is 4.", ex.Message);
//            }
//        }
//        [Test]
//        public async void Execute_CurrentActivityStateIsError_ExpectedException()
//        {
//            string crateStorage = GetCrateStorageAsString();
//
//            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
//            {
//                var processDO = FixtureData.TestProcessExecute();
//                var currAction = FixtureData.TestAction4();
//                currAction.CrateStorage = crateStorage;
//                var nextAction = FixtureData.TestAction5();
//                nextAction.CrateStorage = crateStorage;
//                processDO.CurrentActivity = currAction;
//                processDO.NextActivity = nextAction;
//
//                uow.ProcessRepository.Add(processDO);
//                uow.ActivityRepository.Add(currAction);
//                uow.ActivityRepository.Add(nextAction);
//
//                uow.SaveChanges();
//            }
//            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
//            {
//                var processDO = uow.ProcessRepository.GetByKey(49);
//                var ex = Assert.Throws<Exception>(async () => await _process.Execute(uow, processDO));
//                Assert.AreEqual("Action ID: 3 status is 4.", ex.Message);
//            }
//        }
        [Test]
        public async void Execute_OneActivity_ShouldBeOk()
        {
            string crateStorage = GetCrateStorageAsString();

            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var containerDO = FixtureData.TestContainerExecute();
                var currAction = FixtureData.TestAction4();
                currAction.CrateStorage = crateStorage;
                var nextAction = FixtureData.TestAction5();
                nextAction.CrateStorage = crateStorage;
                containerDO.CurrentRouteNode = currAction;
                containerDO.NextRouteNode = nextAction;

                uow.ContainerRepository.Add(containerDO);
                uow.RouteNodeRepository.Add(currAction);
                uow.RouteNodeRepository.Add(nextAction);

                uow.SaveChanges();
            }
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var containerDO = uow.ContainerRepository.GetByKey(49);
                await _container.Execute(uow, containerDO);

                Assert.IsNull(containerDO.CurrentRouteNode);
               // Assert.IsNull(containerDO.NextActivity);
            }
        }
        [Test]
        public async void Execute_ManyActivities_ShouldBeOk()
        {
            string crateStorage = GetCrateStorageAsString();

            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var containerDO = FixtureData.TestContainerExecute();
                var currActivity = FixtureData.TestActionTreeWithActionTemplates();
                
                containerDO.CurrentRouteNode = currActivity;
                uow.ContainerRepository.Add(containerDO);
                
                AddActionToRepository(uow, currActivity);
                
                uow.SaveChanges();
            }
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var containerDO = uow.ContainerRepository.GetByKey(49);
                await _container.Execute(uow, containerDO);

                Assert.IsNull(containerDO.CurrentRouteNode);
               // Assert.IsNull(processDO.NextActivity);
            }
        }

        private static void AddActionToRepository(IUnitOfWork uow, RouteNodeDO currActivity)
        {
            if (currActivity == null)
                return;
          
            uow.RouteNodeRepository.Add(currActivity);

            if (currActivity.RouteNodes != null)
            {
                foreach (var activity in currActivity.RouteNodes)
                    AddActionToRepository(uow, activity);
            }
        }

        private static string GetCrateStorageAsString()
        {
            List<CrateDTO> curCratesDTO = FixtureData.TestCrateDTO1();
            CrateStorageDTO crateStorageDTO = new CrateStorageDTO();
            crateStorageDTO.CrateDTO.AddRange(curCratesDTO);
            string crateStorage = JsonConvert.SerializeObject(crateStorageDTO);
            return crateStorage;
        }	

    }

}

