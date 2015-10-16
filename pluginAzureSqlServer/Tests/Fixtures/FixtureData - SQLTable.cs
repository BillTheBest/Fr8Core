﻿using Data.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginAzureSqlServer.Tests.Fixtures
{
    partial class FixtureData
    {
        public FixtureData(IUnitOfWork uow)
        {
            _uow = uow;
        }

        private IUnitOfWork _uow;

        public string TestCustomerTable1_Schema()
        {
            return "dbo";
        }

        public string TestCustomerTable1_Table()
        {
            return "Customers";
        }

        public string TestCustomerTable1_Create()
        {
            return @"CREATE TABLE [dbo].[Customers] (
                        [FirstName] NVARCHAR(100) NOT NULL,
                        [LastName] NVARCHAR(100) NOT NULL,
                        PRIMARY KEY CLUSTERED ([FirstName] ASC, [LastName] ASC)
                    )";
        }

        public string TestCustomerTable1_Drop()
        {
            return @"DROP TABLE [dbo].[Customers]";
        }

        public object TestCustomerTable1_Content()
        {
            return new
            {
                Customers = new[]
                {
                    new {firstName = "John", lastName = "Smith"},
                    new {firstName = "Sam", lastName = "Jones"},
                }
            };
        }
    }
}
