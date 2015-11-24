﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using StructureMap;
using Data.Interfaces.DataTransferObjects;
using Hub.Managers.APIManagers.Transmitters.Restful;
using terminalDropbox.Actions;
using terminalDropboxTests.Fixtures;
using TerminalBase.Infrastructure;
using UtilitiesTesting;

namespace terminalDropboxTests.Actions
{
    [TestFixture]
    [Category("terminalDropboxTests")]
    public class GetFileList_v1Tests : BaseTest
    {
        private Get_File_List_v1 _getFileList_v1;

        public override void SetUp()
        {
            base.SetUp();
            TerminalBootstrapper.ConfigureTest();

            var restfulServiceClient = new Mock<IRestfulServiceClient>();
            restfulServiceClient.Setup(r => r.GetAsync<PayloadDTO>(It.IsAny<Uri>()))
                .Returns(Task.FromResult(FixtureData.FakePayloadDTO));
            ObjectFactory.Configure(cfg => cfg.For<IRestfulServiceClient>().Use(restfulServiceClient.Object));

            _getFileList_v1 = ObjectFactory.GetInstance<Get_File_List_v1>();
        }

        [Test]
        public void Run_ReturnsPayloadDTO()
        {
            //Arrange
            var curActionDO = FixtureData.GetFileListTestActionDO1();
            var container = FixtureData.TestContainer();

            //Act
            var payloadDTOResult = _getFileList_v1.Run(curActionDO, container.Id, FixtureData.DropboxAuthorizationToken()).Result;
            var jsonData = ((JValue)(payloadDTOResult.CrateStorage.Crates[0].Contents)).Value.ToString();
            var dropboxFileList = JsonConvert.DeserializeObject<List<string>>(jsonData);
            
            // Assert
            Assert.NotNull(payloadDTOResult);
            Assert.True(dropboxFileList.Any());
        }
    }
}
