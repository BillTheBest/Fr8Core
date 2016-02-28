﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Data.Control;
using Data.Crates;
using Data.Interfaces.DataTransferObjects;
using Data.Interfaces.Manifests;
using HealthMonitor.Utility;
using Hub.Managers;
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
    internal class Convert_TableData_To_AccountingTransactions_v1_Tests : BaseTerminalIntegrationTest
    {
        public override string TerminalName
        {
            get { return "terminalQuickBooks"; }
        }

        [Test, Category("Integration.terminalQuickBooks")]
        public async Task Convert_TableData_To_AccountingTransactions()
        {
            //Arrange
            var configureUrl = GetTerminalConfigureUrl();
            var runUrl = GetTerminalRunUrl();
            var dataDTO =
                HealthMonitor_FixtureData.Convert_TableData_To_AccountingTransactions_v1_InitialConfiguration_Fr8DataDTO();
            var curAccountsCrate = HealthMonitor_FixtureData.ChartOfAccounts_Test1();
            var curTableDataCrate = HealthMonitor_FixtureData.StandardTableData_Test1();
            AddUpstreamCrate(dataDTO, curTableDataCrate, "DocuSignTableDataMappedToQuickbooks");
            using (var crateStorage = Crate.GetUpdatableStorage(dataDTO.ActivityDTO))
            {
                crateStorage.Add(Data.Crates.Crate.FromContent("ChartOfAccounts", curAccountsCrate));
            }
            //Act
            var firstResponseActionDTO = await HttpPostAsync<Fr8DataDTO, ActivityDTO>(
                    configureUrl,
                    dataDTO
                );

            dataDTO.ActivityDTO = firstResponseActionDTO;

            using (var crateStorage = Crate.GetUpdatableStorage(firstResponseActionDTO))
            {
                var controls = crateStorage
                    .CrateContentsOfType<StandardConfigurationControlsCM>()
                    .Single();
                //Set the button group data
                var radioGroup = (RadioButtonGroup)controls.FindByName("Debit/Credit");
                radioGroup.Radios[0].Selected = true;
                radioGroup.Radios[1].Selected = false;
                //Set first distribution line data to controls
                var firstLineGroup = (RadioButtonGroup)controls.FindByName("Line1");
                firstLineGroup.Radios[0].Selected = true;
                firstLineGroup.Radios[1].Selected = false;
                //Set debit account name to the control
                firstLineGroup.Radios[0].Controls[0].Value = "Accounts Payable";
                //Set memo
                var memoTextBox = controls.FindByName("Transaction_Memo");
                memoTextBox.Value = "The testing transactions";
                crateStorage.Remove<StandardAccountingTransactionCM>();
                crateStorage.Add(Data.Crates.Crate.FromContent("StandardConfigurationControlsCM", controls));
                AddOperationalStateCrate(dataDTO, new OperationalStateCM());
            }
            var payloadDTO = await HttpPostAsync<Fr8DataDTO, PayloadDTO>(runUrl, dataDTO);
            AssertControls(Crate.GetByManifest<StandardAccountingTransactionCM>(payloadDTO));
        }

        private void AssertControls(StandardAccountingTransactionCM transactionCrate)
        {
            var firstTransaction = transactionCrate.AccountingTransactions[0];
            var firstLine1 = firstTransaction.FinancialLines[0];
            var secondLine1 = firstTransaction.FinancialLines[1];
            //First transaction data
            Assert.IsTrue(DateTime.Equals(DateTime.Parse("30/12/2015"), firstTransaction.TransactionDate), "The dates are not equal");
            Assert.AreEqual("The testing transactions", firstTransaction.Memo);
            //First transaction, first line
            Assert.AreEqual("Trip to Samarkand", firstLine1.Description);
            Assert.AreEqual("70", firstLine1.Amount);
            Assert.AreEqual("Phone", firstLine1.AccountName);
            Assert.AreEqual("1", firstLine1.AccountId);
            Assert.AreEqual("Credit", firstLine1.DebitOrCredit);
            //First transaction, second line
            Assert.AreEqual("Trip to Samarkand", secondLine1.Description);
            Assert.AreEqual("70", secondLine1.Amount);
            Assert.AreEqual("Accounts Payable", secondLine1.AccountName);
            Assert.AreEqual("Credit", firstLine1.DebitOrCredit);
            Assert.AreEqual("3", secondLine1.AccountId);
            Assert.AreEqual("Debit", secondLine1.DebitOrCredit);

            var secondTransaction = transactionCrate.AccountingTransactions[1];
            var firstLine2 = secondTransaction.FinancialLines[0];
            var secondLine2 = secondTransaction.FinancialLines[1];
            //Second transaction data
            Assert.IsTrue(DateTime.Equals(DateTime.Parse("30/12/2015"), secondTransaction.TransactionDate), "The dates are not equal");
            Assert.AreEqual("The testing transactions", secondTransaction.Memo);
            //Second transaction, first line
            Assert.AreEqual("Trip to Samarkand", firstLine2.Description);
            Assert.AreEqual("90", firstLine2.Amount);
            Assert.AreEqual("Travelling", firstLine2.AccountName);
            Assert.AreEqual("2", firstLine2.AccountId);
            Assert.AreEqual("Credit", firstLine2.DebitOrCredit);
            //Second transaction, second line            
            Assert.AreEqual("Trip to Samarkand", secondLine2.Description);
            Assert.AreEqual("90", secondLine2.Amount);
            Assert.AreEqual("Accounts Payable", secondLine2.AccountName);
            Assert.AreEqual("3", secondLine2.AccountId);
            Assert.AreEqual("Debit", secondLine2.DebitOrCredit);
        }
    }
}
