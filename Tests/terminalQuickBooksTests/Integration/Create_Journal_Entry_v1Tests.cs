﻿using System.Linq;
using System.Threading.Tasks;
using Data.Control;
using Data.Crates;
using Data.Interfaces.DataTransferObjects;
using Data.Interfaces.Manifests;
using HealthMonitor.Utility;
using NUnit.Framework;
using terminalQuickBooksTests.Fixtures;

namespace terminalQuickBooksTests.Integration
{
    /// <summary>
    /// Mark test case class with [Explicit] attiribute.
    /// It prevents test case from running when CI is building the solution,
    /// but allows to trigger that class from HealthMonitor.
    /// </summary>
    [Explicit]
    internal class Create_Journal_Entry_v1Tests : BaseTerminalIntegrationTest
    {
        public override string TerminalName
        {
            get { return "terminalQuickBooks"; }
        }
        [Test, Category("Integration.terminalQuickBooks")]
        public async Task Create_Journal_Entry_Configuration_Check_With_No_Upstream_Crate()
        {
            //Arrange
            var curMessage =
                "When this Action runs, it will be expecting to find a Crate of Standard Accounting Transactions. " +
                "Right now, it doesn't detect any Upstream Actions that produce that kind of Crate. " +
                "Please add an activity upstream (to the left) of this action that does so.";
            var configureUrl = GetTerminalConfigureUrl();
            var requestActionDTO = HealthMonitor_FixtureData.Action_Create_Journal_Entry_v1_InitialConfiguration_Fr8DataDTO();
            //Act
            var responseActionDTO = await HttpPostAsync<Fr8DataDTO, ActivityDTO>(
                    configureUrl,
                    requestActionDTO
                );
            //Assert
            Assert.NotNull(responseActionDTO);
            Assert.NotNull(responseActionDTO.CrateStorage);
            Assert.NotNull(responseActionDTO.CrateStorage.Crates);
            var crateStorage = Crate.FromDto(responseActionDTO.CrateStorage);
            var curTextBlock = (TextBlock)crateStorage.CrateContentsOfType<StandardConfigurationControlsCM>().Single().Controls[0];
            Assert.AreEqual("Create a Journal Entry", curTextBlock.Label);
            Assert.AreEqual(curMessage, curTextBlock.Value);
            Assert.AreEqual("alert alert-warning", curTextBlock.CssClass);
        }
        [Test, Category("Integration.terminalQuickBooks")]
        public async Task Create_Journal_Entry_Configuration_Check_With_Upstream_Crate()
        {
            //Arrange
            var configureUrl = GetTerminalConfigureUrl();
            var dataDTO = HealthMonitor_FixtureData.Action_Create_Journal_Entry_v1_InitialConfiguration_Fr8DataDTO();
            var curStandAccTransCrate = HealthMonitor_FixtureData.GetAccountingTransactionCM();
            AddUpstreamCrate(dataDTO, curStandAccTransCrate);
            //Act
            var responseActionDTO = await HttpPostAsync<Fr8DataDTO, ActivityDTO>(
                    configureUrl,
                    dataDTO
                );
            //Assert
            Assert.NotNull(responseActionDTO);
            Assert.NotNull(responseActionDTO.CrateStorage);
            Assert.NotNull(responseActionDTO.CrateStorage.Crates);
            var crateStorage = Crate.FromDto(responseActionDTO.CrateStorage);
            var curTextBlock = (TextBlock)crateStorage.CrateContentsOfType<StandardConfigurationControlsCM>().Single().Controls[0];
            Assert.AreEqual("Create a Journal Entry", curTextBlock.Label);
            Assert.AreEqual("This Action doesn't require any configuration.", curTextBlock.Value);
            Assert.AreEqual("well well-lg", curTextBlock.CssClass);
        }
    }
}
