﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Data.Control;
using Data.Crates;
using Data.Entities;
using Data.Infrastructure.AutoMapper;
using Data.Interfaces.DataTransferObjects;
using Data.Interfaces.Manifests;
using Data.States;

using Hub.Interfaces;
using Hub.Managers;
using Hub.Managers.APIManagers.Transmitters.Restful;
using Hub.StructureMap;
using Moq;
using NUnit.Framework;
using SendGrid;
using StructureMap;
using terminalSendGrid.Actions;
using terminalSendGrid.Infrastructure;
using terminalSendGrid.Services;
using terminalSendGrid.Tests.Fixtures;
using TerminalBase.Infrastructure;
using Utilities;

namespace terminalSendGrid.Tests.Actions
{
    [TestFixture]
    [Category("terminalSendGrid")]
    public class SendEmailViaSendGrid_v1Tests : BaseTest
    {
        private SendEmailViaSendGrid_v1 _gridAction;
        private ICrateManager _crate;
        private ActionDTO actionDto;

        public override void SetUp()
        {
            base.SetUp();

            const StructureMapBootStrapper.DependencyType dependencyType = StructureMapBootStrapper.DependencyType.TEST;

            DataAutoMapperBootStrapper.ConfigureAutoMapper();
            StructureMapBootStrapper.ConfigureDependencies(dependencyType).SendGridConfigureDependencies(dependencyType);
            ObjectFactory.Configure(cfg => cfg.For<ITransport>().Use(c => TransportFactory.CreateWeb(c.GetInstance<IConfigRepository>())));
            ObjectFactory.Configure(cfg => cfg.For<IEmailPackager>().Use(new SendGridPackager()));
            TerminalBootstrapper.ConfigureTest();

            _crate = ObjectFactory.GetInstance<ICrateManager>();

            var routeNode = new Mock<IRouteNode>();
            routeNode.Setup(c => c.GetCratesByDirection<StandardDesignTimeFieldsCM>(It.IsAny<Guid>(), It.IsAny<CrateDirection>()))
                    .Returns(Task.FromResult(new List<Crate<StandardDesignTimeFieldsCM>>()));

            ObjectFactory.Configure(cfg => cfg.For<IRouteNode>().Use(routeNode.Object));

            actionDto = GetActionResult();
            var payLoadDto = FixtureData.CratePayloadDTOForSendEmailViaSendGridConfiguration;
            payLoadDto.CrateStorage = actionDto.CrateStorage;

            using (var updater = new CrateManager().UpdateStorage(payLoadDto))
            {
                var operationalStatus = new OperationalStateCM();
                var operationsCrate = Crate.FromContent("Operational Status", operationalStatus);
                updater.CrateStorage.Add(operationsCrate);
            }

            var restfulServiceClient = new Mock<IRestfulServiceClient>();
            restfulServiceClient.Setup(r => r.GetAsync<PayloadDTO>(It.IsAny<Uri>(), It.IsAny<string>()))
                .Returns(Task.FromResult(payLoadDto));
            ObjectFactory.Configure(cfg => cfg.For<IRestfulServiceClient>().Use(restfulServiceClient.Object));
        }

        [Test]
        public void Configure_ReturnsCrateDTO()
        {
            // Act
            var controlsCrates = actionDto.CrateStorage.Crates;

            // Assert
            Assert.IsNotNull(controlsCrates);
            Assert.AreEqual(controlsCrates.Count(), 2);
        }

        [Test]
        public void Configure_ReturnsCrateDTOStandardConfigurationControlsMS()
        {
            // Act
            var controlsCrate = _crate.FromDto(actionDto.CrateStorage).CratesOfType<StandardConfigurationControlsCM>().FirstOrDefault();

            // Assert
            Assert.IsNotNull(controlsCrate);
            Assert.IsNotNull(controlsCrate.ManifestType);
            Assert.IsNotNull(controlsCrate.Content);
            Assert.AreEqual(controlsCrate.Content.Controls.Count, 3);
        }

        [Test]
        public void Configure_ReturnsEmailControls()
        {
            // Act && Assert
            var standardControls = _crate.FromDto(actionDto.CrateStorage).CrateContentsOfType<StandardConfigurationControlsCM>().FirstOrDefault();
            Assert.IsNotNull(standardControls);

            var controls = standardControls.Controls.Where(a => a.Type == "TextSource").Count();

            Assert.AreEqual(3, controls);
        }

        private ActionDTO GetActionResult()
        {
            _gridAction = new SendEmailViaSendGrid_v1();
            var curActionDO = FixtureData.ConfigureSendEmailViaSendGridAction();
            ActionDTO curActionDTO = Mapper.Map<ActionDTO>(curActionDO);
            var curAuthTokenDO = Mapper.Map<AuthorizationTokenDO>(curActionDTO.AuthToken);
            var actionResult = _gridAction.Configure(curActionDO, curAuthTokenDO).Result;
            return Mapper.Map<ActionDTO>(actionResult);
        }

        [Test]
        public void Run_Returns_PayloadDTO()
        {
            // Arrange
            ICrateManager Crate = ObjectFactory.GetInstance<ICrateManager>();
            _gridAction = new SendEmailViaSendGrid_v1();
            var curActionDO = FixtureData.ConfigureSendEmailViaSendGridAction();

            ActionDTO curActionDTO = Mapper.Map<ActionDTO>(curActionDO);
            curActionDTO.CrateStorage = actionDto.CrateStorage;
            var curAuthTokenDO = Mapper.Map<AuthorizationTokenDO>(curActionDTO.AuthToken);
            var actionDO = Mapper.Map<ActionDO>(curActionDTO);

            //updating controls
            var standardControls = _crate.FromDto(actionDto.CrateStorage).CrateContentsOfType<StandardConfigurationControlsCM>().FirstOrDefault();
            foreach (TextSource control in standardControls.Controls)
            {
                control.ValueSource = "specific";
                control.Value = (control.Name == "EmailAddress") ? "test@mail.com" : "test";
            }
            var crate = Crate.CreateStandardConfigurationControlsCrate("SendGrid", standardControls.Controls.ToArray());

            using (var updater = Crate.UpdateStorage(actionDO))
            {
                updater.CrateStorage.RemoveByManifestId(6);
                updater.CrateStorage.Add(crate);
            }

            // Act
            var payloadDTOResult = _gridAction.Run(actionDO, curActionDTO.ContainerId, curAuthTokenDO).Result;

            // Assert
            Assert.NotNull(payloadDTOResult);
        }
    }
}
