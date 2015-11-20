﻿using System;
using Data.Crates;
using Data.Entities;
using Data.Interfaces.DataTransferObjects;
using Data.Interfaces.Manifests;
using Newtonsoft.Json;

namespace terminalDropboxTests.Fixtures
{
    public class FixtureData
    {
        public static Guid TestGuid_Id_333()
        {
            return new Guid("8339DC87-F011-4FB1-B47C-FEC406E4100A");
        }

        public static AuthorizationTokenDO DropboxAuthorizationToken()
        {
            return new AuthorizationTokenDO()
            {
                Token = "bLgeJYcIkHAAAAAAAAAAFf6hjXX_RfwsFNTfu3z00zrH463seBYMNqBaFpbfBmqf"
            };
        }

        public static ActionDO GetFileListTestActionDO1()
        {
            var actionTemplate = GetFileListTestActivityTemplateDO();

            var actionDO = new ActionDO()
            {
                Name = "testaction",
                Id = TestGuid_Id_333(),
                ActivityTemplateId = actionTemplate.Id,
                ActivityTemplate = actionTemplate,
                CrateStorage = "",
                
            };
            return actionDO;
        }

        public static Guid TestContainerGuid()
        {
            return new Guid("70790811-3394-4B5B-9841-F26A7BE35163");
        }

        public static ContainerDO TestContainer()
        {
            var containerDO = new ContainerDO();
            containerDO.Id = TestContainerGuid();
            containerDO.ContainerState = 1;
            return containerDO;
        }

        public static ActivityTemplateDO GetFileListTestActivityTemplateDO()
        {
            return new ActivityTemplateDO
            {
                Id = 1,
                Name = "Get File List",
                Version = "1"
            };
        }

        public static PayloadDTO FakePayloadDTO
        {
            get
            {
                PayloadDTO payloadDTO = new PayloadDTO(TestContainerGuid());
                return payloadDTO;
            }
        }
    }
}
