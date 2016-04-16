﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Data.Entities;
using Data.Interfaces.DataTransferObjects;
using Data.Interfaces.Manifests;
using Data.States;
using Hub.Interfaces;
using Hub.Managers.APIManagers.Transmitters.Restful;
using Moq;
using NUnit.Framework;
using StructureMap;
using TerminalBase.Infrastructure;
using UtilitiesTesting;

namespace terminaBaselTests.Infrastructure
{
    [TestFixture]
    [Category("DefaultHubCommunicator")]
    public class DefaultHubCommunicatorTests : BaseTest
    {
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
        }

        [Test]
        public void GetAvailableData_ShouldGenerateCorrectDesigntimeURL()
        {
            var _restfulServiceClient = new Mock<IRestfulServiceClient>();
            _restfulServiceClient.Setup(r => r.GetAsync<AvailableDataDTO>(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()));
            ObjectFactory.Configure(cfg => cfg.For<IRestfulServiceClient>().Use(_restfulServiceClient.Object));
            IHubCommunicator _hubCommunicator = new DefaultHubCommunicator();
            _hubCommunicator.Configure("sampleterminal");

            Guid id = Guid.NewGuid();
            CrateDirection direction = CrateDirection.Downstream;
            AvailabilityType availability = AvailabilityType.RunTime;

            string resultUrl = String.Format("http://localhost:30643/api/v1/plannodes/available_data?id={0}&direction={1}&availability={2}",
                id.ToString(),
                ((int)direction).ToString(),
                ((int)availability).ToString());
            _hubCommunicator.GetAvailableData(new ActivityDO() {Id = id}, direction, availability, null);

            _restfulServiceClient.Verify(o => o.GetAsync<AvailableDataDTO>(It.Is<Uri>(p => p.ToString() == resultUrl), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()));
        }
    }
}
