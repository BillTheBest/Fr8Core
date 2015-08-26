﻿using Data.Entities;
using Data.States;
using System.Collections.Generic;

namespace UtilitiesTesting.Fixtures
{
    public partial class FixtureData
    {
        public static List<DocuSignTemplateSubscriptionDO> DocuSignTemplateSubscriptionList1()
        {
            return new List<DocuSignTemplateSubscriptionDO>()
            {
                new DocuSignTemplateSubscriptionDO()
                {
                    Id = 1,
                    ProcessTemplateId = 1,
                    DocuSignTemplateId = "AAA"
                },
                new DocuSignTemplateSubscriptionDO()
                {
                    Id = 2,
                    ProcessTemplateId = 1,
                    DocuSignTemplateId = "BBB"
                },
                new DocuSignTemplateSubscriptionDO()
                {
                    Id = 3,
                    ProcessTemplateId = 1,
                    DocuSignTemplateId = "CCC"
                },
                new DocuSignTemplateSubscriptionDO()
                {
                    Id = 4,
                    ProcessTemplateId = 1,
                    DocuSignTemplateId = "DDD"
                },
                new DocuSignTemplateSubscriptionDO()
                {
                    Id = 5,
                    ProcessTemplateId = 1,
                    DocuSignTemplateId = "EEE"
                }
            };
        }

        public static List<DocuSignTemplateSubscriptionDO> DocuSignTemplateSubscriptionList2()
        {
            return new List<DocuSignTemplateSubscriptionDO>()
            {
                new DocuSignTemplateSubscriptionDO()
                {
                    Id = 1,
                    ProcessTemplateId = 1,
                    DocuSignTemplateId = "AAA"
                },
                new DocuSignTemplateSubscriptionDO()
                {
                    Id = 2,
                    ProcessTemplateId = 1,
                    DocuSignTemplateId = "BBB"
                },
                new DocuSignTemplateSubscriptionDO()
                {
                    Id = 3,
                    ProcessTemplateId = 1,
                    DocuSignTemplateId = "CCC"
                },
                new DocuSignTemplateSubscriptionDO()
                {
                    Id = 0,
                    ProcessTemplateId = 1,
                    DocuSignTemplateId = "KKK"
                },
                new DocuSignTemplateSubscriptionDO()
                {
                    Id = 0,
                    ProcessTemplateId = 1,
                    DocuSignTemplateId = "MMM"
                }
            };
        }
    }
}