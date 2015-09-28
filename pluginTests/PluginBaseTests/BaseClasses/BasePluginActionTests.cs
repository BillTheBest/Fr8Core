﻿using AutoMapper;
using Core.Interfaces;
using Data.Entities;
using Data.Interfaces;
using Data.Interfaces.DataTransferObjects;
using Data.Interfaces.ManifestSchemas;
using Newtonsoft.Json;
using NUnit.Framework;
using pluginAzureSqlServer.Actions;
using PluginBase.BaseClasses;
using PluginBase.Infrastructure;
using StructureMap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UtilitiesTesting;
using UtilitiesTesting.Fixtures;

namespace pluginTests.PluginBaseTests.Controllers
{

    [TestFixture]
    [Category("BasePluginAction")]
    public class BasePluginActionTests : BaseTest
    {
        BasePluginAction _basePluginAction;

        public BasePluginActionTests()
        {
            base.SetUp();
            _basePluginAction = new BasePluginAction();
        }


        [Test]
        public void ProcessConfigurationRequest_CrateStroageIsNull_ShouldNotCrateStorage()
        {
            //Arrange
            ActionDTO curActionDTO = FixtureData.TestActionDTO1();
            ConfigurationEvaluator curConfigurationEvaluator = EvaluateReceivedRequest;
            object[] parameters = new object[] { curActionDTO, curConfigurationEvaluator };

            //Act
            var result = (ActionDTO)HelperLibrary.InvokeClassMethod(typeof(BasePluginAction), "ProcessConfigurationRequest", parameters);

            //Assert
            Assert.AreEqual(curActionDTO.CrateStorage.CrateDTO.Count, result.CrateStorage.CrateDTO.Count);
        }


        [Test]
        public void ProcessConfigurationRequest_ConfigurationRequestTypeIsFollowUp_ReturnsExistingCrateStorage()
        {
            //Arrange
            ActionDO curAction = FixtureData.TestConfigurationSettingsDTO1();
            ActionDTO curActionDTO = Mapper.Map<ActionDTO>(curAction);
            ConfigurationEvaluator curConfigurationEvaluator = EvaluateReceivedRequest;

            object[] parameters = new object[] { curActionDTO, curConfigurationEvaluator };

            //Act
            var result = (ActionDTO)HelperLibrary.InvokeClassMethod(typeof(BasePluginAction), "ProcessConfigurationRequest", parameters);

            //Assert
            Assert.AreEqual(curActionDTO.CrateStorage.CrateDTO.Count, result.CrateStorage.CrateDTO.Count);
            Assert.AreEqual(curActionDTO.CrateStorage.CrateDTO[0].ManifestType, result.CrateStorage.CrateDTO[0].ManifestType);

        }

        [Test]
        public void PackControlsCrate_ReturnsStandardConfigurationControls()
        {
            //Arrange
            object[] parameters = new object[] { FixtureData.FieldDefinitionDTO1() };

            ;
            //Act
            var result = (CrateDTO)HelperLibrary.InvokeClassMethod(typeof(BasePluginAction), "PackControlsCrate", parameters);

            //Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(CrateManifests.STANDARD_CONF_CONTROLS_NANIFEST_NAME, result.ManifestType);
        }


        [Test]
        public void MergeContentFields_ReturnsStandardDesignTimeFieldsMS()
        {
            var result = _basePluginAction.MergeContentFields(FixtureData.TestCrateDTO1());
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Fields.Count);
        }



        //TestActionTree
        [Test]
        public void GetDesignTimeFields_CrateDirectionIsUpstream_ReturnsMergeDesignTimeFields()
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                uow.ActivityRepository.Add(FixtureData.TestActionTree());
                uow.SaveChanges();

                ActionDO curAction = FixtureData.TestAction57();

                var result = _basePluginAction.GetDesignTimeFields(curAction, BasePluginAction.GetCrateDirection.Upstream);
                Assert.NotNull(result);
                Assert.AreEqual(16, result.Fields.Count);

            }
        }

        [Test]
        public void GetDesignTimeFields_CrateDirectionIsDownstream_ReturnsMergeDesignTimeFields()
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                uow.ActivityRepository.Add(FixtureData.TestActionTree());
                uow.SaveChanges();

                ActionDO curAction = FixtureData.TestAction57();

                var result = _basePluginAction.GetDesignTimeFields(curAction, BasePluginAction.GetCrateDirection.Downstream);
                Assert.NotNull(result);
                Assert.AreEqual(18, result.Fields.Count);
            }
        }


        private ConfigurationRequestType EvaluateReceivedRequest(ActionDTO curActionDTO)
        {
            CrateStorageDTO curCrates = curActionDTO.CrateStorage;
            if (curCrates.CrateDTO.Count == 0)
                return ConfigurationRequestType.Initial;
            return ConfigurationRequestType.Followup;
        }

       

    }
}
