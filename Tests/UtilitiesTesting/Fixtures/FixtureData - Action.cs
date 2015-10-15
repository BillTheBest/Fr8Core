﻿using Core.Interfaces;
using Data.Entities;
using Data.Interfaces;
using Data.Interfaces.DataTransferObjects;
using Data.States;
using Newtonsoft.Json;
using StructureMap;
using System.Collections.Generic;
using System;
using Data.Interfaces.ManifestSchemas;

namespace UtilitiesTesting.Fixtures
{
    partial class FixtureData
    {
        public static ActionDO TestActionWriteSqlServer1()
        {
            var actionTemplate = ActionTemplate();

            var curActionDO = new ActionDO
            {
                Id = 54,
                Name = "Write to Sql Server",
                CrateStorage = "",
                Ordering = 1,
                ActionState = ActionState.Unstarted,
                ActivityTemplateId = actionTemplate.Id,
                ActivityTemplate = actionTemplate
            };
            return curActionDO;
        }

        public static ActivityTemplateDO ActionTemplate()
        {
            return new ActivityTemplateDO()
            {
                Id = 1,
                Name = "Send an Email",
                Plugin = new PluginDO { Name = "Send an Email", Version = "1", Endpoint = "", PluginStatus = PluginStatus.Active },

                Version = "1"
            };
        }

        public static ActivityTemplateDO ActivityTemplateSMS()
        {
            return new ActivityTemplateDO()
            {
                Id = 1,
                Name = "Send a Text (SMS) Message",
                Plugin = new PluginDO { Name = "Send a Text (SMS) Message", Version = "1", Endpoint = "", PluginStatus = PluginStatus.Active },
                Version = "1"
            };
        }

        public static ActionDO TestAction1()
        {
            var actionTemplate = ActionTemplate();
            var curActionDO = new ActionDO
            {
                Name = "Action 1",
                ActivityTemplateId = actionTemplate.Id,
                ActivityTemplate = actionTemplate,
            };
            return curActionDO;
        }

        public static ActionDO TestAction2()
        {
            var actionTemplate = ActionTemplate();
            var curActionDO = new ActionDO
            {
                Id = 2,
                Name = "Action 2",
                ActivityTemplateId = actionTemplate.Id,
                ActivityTemplate = actionTemplate
            };
            return curActionDO;
        }

        public ActionDO TestAction3()
        {
            var actionTemplate = ActionTemplate();
            var origActionDO = new ActionDO()
            {
                ParentActivityId = null,
                Name = "type 1",
                Id = 34,
                CrateStorage = "config settings",

                Ordering = 3,
                ActivityTemplateId = actionTemplate.Id,
                ActivityTemplate = actionTemplate
            };

            return origActionDO;
        }

        public static ActionDO TestAction4()
        {
            var actionTemplate = ActionTemplate();
            var curActionDO = new ActionDO
            {
                Id = 3,
                Name = "Send an Email",
                ActivityTemplateId = actionTemplate.Id,
                ActivityTemplate = actionTemplate
            };
            return curActionDO;
        }

        public static ActionDO TestAction5()
        {
            var actionTemplate = ActivityTemplateSMS();
            var curActionDO = new ActionDO
            {
                Id = 4,
                Name = "Send a Text (SMS) Message",
                ActivityTemplateId = actionTemplate.Id,
                ActivityTemplate = actionTemplate
            };
            return curActionDO;
        }

        public static ActionDO TestAction6()
        {
            var actionTemplate = ActionTemplate();
            actionTemplate.Name = null;

            return new ActionDO
            {
                Id = 6,
                ParentActivityId = 1,
                Ordering = 2,
                ActionState = ActionState.Unstarted,
                ActivityTemplateId = actionTemplate.Id,
                ActivityTemplate = actionTemplate
            };
        }

        public static ActionDO TestAction7()
        {
            var actionTemplate = ActionTemplate();
            return new ActionDO
            {
                Id = 7,
                Name = "Action 7",
                ParentActivityId = 1,
                Ordering = 3,
                ActionState = ActionState.Unstarted,
                ActivityTemplateId = actionTemplate.Id,
                ActivityTemplate = actionTemplate
            };
        }

        public static ActionDO TestAction8(ActivityDO parentActivity)
        {
            var actionTemplate = ActionTemplate();
            return new ActionDO
            {
                Id = 8,
                Name = "Action 8",
                ParentActivityId = 1,
                Ordering = 4,
                ActionState = ActionState.Unstarted,
                ParentActivity = parentActivity,

                ActivityTemplateId = actionTemplate.Id,
                ActivityTemplate = actionTemplate
            };
        }

        public static ActionDO TestAction10()
        {
            var actionTemplate = ActionTemplate();
            return new ActionDO
            {
                Id = 5,
                Name = "Action 5",
                ParentActivityId = 1,
                Ordering = 1,
                ActionState = ActionState.Unstarted,
                ActivityTemplateId = actionTemplate.Id,
                ActivityTemplate = actionTemplate
            };
        }

        public static ActionDO TestAction9()
        {
            var actionTemplate = ActionTemplate();
            return new ActionDO
            {
                Id = 2,
                ActionState = ActionState.Error,
                ActivityTemplateId = actionTemplate.Id,
                ActivityTemplate = actionTemplate
            };
        }

        public static ActionDO TestAction20()
        {
            var actionTemplate = ActionTemplate();
            return new ActionDO
            {
                Id = 1,
                Name = "Action 1",
                ParentActivityId = 1,
                Ordering = 1,
                ActivityTemplateId = actionTemplate.Id,
                ActivityTemplate = actionTemplate
            };
        }

        public static ActionDO TestAction21()
        {
            var actionTemplate = ActionTemplate();
            return new ActionDO
            {
                Id = 2,
                Name = "Action 2",
                ParentActivityId = 1,
                Ordering = 2,
                ActivityTemplateId = actionTemplate.Id,
                ActivityTemplate = actionTemplate
            };
        }

        public static ActionDO TestAction22()
        {
            var actionTemplate = FixtureData.ActionTemplate();

            return new ActionDO
            {
                Id = 10,
                Name = "WriteToAzureSql",
                ParentActivityId = 1,
                CrateStorage = "JSON Config Settings",

                Ordering = 1,
                ActionState = ActionState.Unstarted,
                ActivityTemplateId = actionTemplate.Id,
                ActivityTemplate = actionTemplate

            };
        }

        public static ActionDO TestAction23()
        {
            var actionTemplate = FixtureData.TestActivityTemplateDO1();
            return new ActionDO
            {
                Id = 2,
                Name = "Action 2",
                Ordering = 2,
                ActivityTemplateId = actionTemplate.Id,
                ActivityTemplate = actionTemplate
            };
        }

        public static ActionDO IntegrationTestAction()
        {
            //string envelopeId = "F02C3D55-F6EF-4B2B-B0A0-02BF64CA1E09";
            var actionTemplate = ActionTemplate();


            var processTemplateDo = TestRoute2();

            var processDo = new ContainerDO()
            {
                Id = 1,
                CrateStorage = EnvelopeIdCrateJson(),
                ContainerState = 1,
                RouteId = processTemplateDo.Id,
                Route = processTemplateDo
            };

            var subrouteDo = new SubrouteDO()
            {
                Id = 1,
                Name = "C",
                ParentActivityId = processTemplateDo.Id,
                ParentActivity = processTemplateDo
            };


            var actionDo = new ActionDO()
            {
                ParentActivity = subrouteDo,
                ParentActivityId = 1,
                ActionState = ActionState.Unstarted,
                Name = "testaction",

                Id = 1,
                ActivityTemplateId = actionTemplate.Id,
                ActivityTemplate = actionTemplate,
                CrateStorage = EnvelopeIdCrateJson()
            };

            return actionDo;
        }

        public static CrateDTO GetEnvelopeIdCrate(string curEnvelopeId = "11f41f43-57bd-4568-86f5-9ceabdaafc43")
        {
            var crateFields = new List<FieldDTO>()
                    {
                        new FieldDTO() { Key = "EnvelopeId", Value = curEnvelopeId },
                        new FieldDTO() { Key = "ExternalEventType", Value = "1" },
                        new FieldDTO() { Key = "RecipientId", Value= "1" }
                    };
            var curEventData = new CrateDTO()
            {
                Contents = JsonConvert.SerializeObject(crateFields),
                Label = "Event Data",
                Id = Guid.NewGuid().ToString()
            };

            return curEventData;
        }

        public static string EnvelopeIdCrateJson()
        {
            return JsonConvert.SerializeObject(GetEnvelopeIdCrate());
        }

        public static ActionDO TestActionHealth1()
        {
            var actionDo = new ActionDO
            {
                Id = 1,

                ActionState = ActionState.Unstarted,
                Name = "testaction",
                CrateStorage = "config settings",
                ParentActivityId = 88,
                ActivityTemplateId = FixtureData.TestActivityTemplate1().Id
            };
            return actionDo;
        }

        public static ActionDO TestActionUnstarted()
        {
            var actionTemplate = ActionTemplate();
            //string envelopeId = "F02C3D55-F6EF-4B2B-B0A0-02BF64CA1E09";

            var processTemplateDo = new RouteDO()
            {
                Id = 1,
                Name = "A",
                Description = "B",
                RouteState = RouteState.Active
            };

            var processDo = new ContainerDO()
            {
                Id = 1,
                CrateStorage = EnvelopeIdCrateJson(),
                ContainerState = 1,
                RouteId = processTemplateDo.Id,
                Route = processTemplateDo
            };

            var subrouteDo = new SubrouteDO()
            {
                Id = 1,
                Name = "C",
                ParentActivityId = processTemplateDo.Id,
                ParentActivity = processTemplateDo
            };


            return new ActionDO
            {
                Id = 1,
                ActionState = ActionState.Unstarted,
                Name = "testaction",
                ParentActivity = processTemplateDo,

                ActivityTemplateId = actionTemplate.Id,
                ActivityTemplate = actionTemplate
            };
        }

        public static ActionDO TestActionAuthenticate1()
        {
            PluginDO curPluginDO = new PluginDO()
            {
                Id = 1,
                Name = "AzureSqlServer",
                PluginStatus = 1,
                Version = "1"
            };

            ActivityTemplateDO curActivityTemplateDO = new ActivityTemplateDO
            {
                Id = 1,
                //ActionType = "Write to Sql Server",
                //ParentPluginRegistration = "pluginAzureSqlServer",
                Version = "v1",
                AuthenticationType = "OAuth",
                Plugin = curPluginDO,
                PluginID = 1,
            };



            var curRouteDO = new RouteDO
            {
                Id = 1,
                Description = "descr 1",
                Name = "template1",
                RouteState = RouteState.Active,
                DockyardAccount = FixtureData.TestDockyardAccount1()
            };

            var curProcessDO = new ContainerDO()
            {
                Id = 1,
                RouteId = 1,
                Route = curRouteDO
            };


            var subroute = new SubrouteDO(true)
            {
                ParentActivity = curRouteDO,
                ParentActivityId = curRouteDO.Id,
            };

            ActionDO curActionDO = new ActionDO();
            curActionDO.Id = 3;
            curActionDO.ParentActivity = subroute;
            curActionDO.ParentActivityId = subroute.Id;
            curActionDO.ActivityTemplateId = 1;
            curActionDO.ActivityTemplate = curActivityTemplateDO;
            curActionDO.ActionState = 1;
            curActionDO.Name = "testaction";

            subroute.Activities.Add(curActionDO);

            //  curActionDO.ConfigurationSettings = "config settings";
            //  curActionDO.ParentActionListId = 1;

            // curActionListDO.Actions.Add(curActionDO);

            //   curActionDO.ParentActionList = curActionListDO;



            return curActionDO;
        }

        public static ActionDO WaitForDocuSignEvent_Action()
        {
            string templateId = "58521204-58af-4e65-8a77-4f4b51fef626";
            var actionTemplate = ActionTemplate();
            ICrate _crate = ObjectFactory.GetInstance<ICrate>();
            IAction _action = ObjectFactory.GetInstance<IAction>();

            var fieldSelectDockusignTemplate = new DropDownListControlDefinitionDTO()
            {
                Label = "Select DocuSign Template",
                Name = "Selected_DocuSign_Template",
                Required = true,
                Value = templateId,
                Events = new List<ControlEvent>() {
                     new ControlEvent("onSelect", "requestConfiguration")
                }
            };

            var actionDo = new ActionDO()
            {

                ActionState = ActionState.Unstarted,
                Name = "testaction",

                Id = 1,
                ActivityTemplateId = actionTemplate.Id,
                ActivityTemplate = actionTemplate
            };

            var fields = new List<ControlDefinitionDTO>()
            {
                fieldSelectDockusignTemplate
            };

            var crateConfiguration = new List<CrateDTO>()
            {
                _crate.Create("Configuration_Controls", JsonConvert.SerializeObject(fields)),
            };

            _action.AddCrate(actionDo, crateConfiguration);

            return actionDo;
        }

        public static AuthorizationTokenDO TestActionAuthenticate2()
        {
            AuthorizationTokenDO curAuthorizationTokenDO = new AuthorizationTokenDO()
            {
                Token = "TestToken",
                AuthorizationTokenState = AuthorizationTokenState.Active
            };
            return curAuthorizationTokenDO;
        }

        public static AuthorizationTokenDO TestActionAuthenticate3()
        {
            AuthorizationTokenDO curAuthorizationTokenDO = new AuthorizationTokenDO()
            {
                Token = "TestToken",
                AuthorizationTokenState = AuthorizationTokenState.Revoked
            };
            return curAuthorizationTokenDO;
        }

        public static ActionDO TestAction57()
        {
            return new ActionDO()
            {
                Id = 57,
                Ordering = 2,
                ParentActivityId = 54
            };

        }       

        public static ActionDO TestActionTree()
        {
            List<CrateDTO> curCratesDTO = FixtureData.TestCrateDTO1();
            CrateStorageDTO crateStorageDTO = new CrateStorageDTO();
            crateStorageDTO.CrateDTO.AddRange(curCratesDTO);
            string crateStorage = JsonConvert.SerializeObject(crateStorageDTO);

            
            ActionDO curAction = new ActionDO()
            {
                Id = 1,
                Ordering = 1,
                 CrateStorage=  crateStorage,
                 
                Activities = new List<ActivityDO>
                {
                    new ActionDO
                    {
                        Id = 23,
                        Ordering = 1,
                        ParentActivityId = 1,
                         CrateStorage=  crateStorage
                    },
                    new ActionDO
                    {
                        Id = 43,
                        ParentActivityId = 1,
                        Ordering = 2,
                         CrateStorage=  crateStorage,
                        Activities = new List<ActivityDO>
                        {
                            new ActionDO
                            {
                                Id = 44,
                                Ordering = 1,
                                ParentActivityId = 43,
                                CrateStorage=  crateStorage
                            },
                            new ActionDO
                            {
                                Id = 46,
                                Ordering = 2,
                                ParentActivityId = 43,
                                CrateStorage=  crateStorage
                            },
                            new ActionDO
                            {
                                Id = 48,
                                Ordering = 3,
                                ParentActivityId = 43,
                                CrateStorage=  crateStorage
                            },

                        }
                    },
                    new ActivityDO
                    {
                        Id = 52,
                        Ordering = 3,
                        ParentActivityId = 1,
                        Activities = new List<ActivityDO>
                        {
                            new ActionDO
                            {
                                Id = 53,
                                Ordering = 1,
                                ParentActivityId = 52,
                                CrateStorage=  crateStorage
                            },
                            new ActivityDO
                            {
                                Id = 54,
                                ParentActivityId = 52,
                                Ordering = 2,

                                Activities = new List<ActivityDO>
                                {
                                    new ActionDO
                                    {
                                        Id = 56,
                                        ParentActivityId = 54,
                                        Ordering = 1,
                                CrateStorage=  crateStorage
                                    },
                                    new ActionDO
                                    {
                                        Id = 57,
                                        ParentActivityId = 54,
                                        Ordering = 2,
                                CrateStorage=  crateStorage
                                    },
                                    new ActionDO
                                    {
                                        Id = 58,
                                        ParentActivityId = 54,
                                        Ordering = 3,
                                CrateStorage=  crateStorage
                                    },

                                }
                            },
                            new ActionDO
                            {
                                Id = 55,
                                ParentActivityId = 52,
                                Ordering = 3,
                                CrateStorage=  crateStorage
                            },

                        }
                    },
                    new ActionDO
                    {
                        Id = 59,
                        Ordering = 4,
                        ParentActivityId = 1,
                         CrateStorage=  crateStorage,
                        Activities = new List<ActivityDO>
                        {
                            new ActionDO
                            {
                                Id = 60,
                                ParentActivityId = 59,
                                Ordering = 1,
                                CrateStorage=  crateStorage
                            },
                            new ActionDO
                            {
                                Id = 61,
                                ParentActivityId = 59,
                                Ordering = 2,
                                CrateStorage=  crateStorage,
                                Activities = new List<ActivityDO>
                                {
                                    new ActionDO
                                    {
                                        Id = 63,
                                        ParentActivityId = 61,
                                        Ordering = 1,
                                CrateStorage=  crateStorage
                                    },
                                    new ActionDO
                                    {
                                        Id = 64,
                                        ParentActivityId = 61,
                                        Ordering = 2,
                                CrateStorage=  crateStorage
                                    },
                                    new ActionDO
                                    {
                                        Id = 65,
                                        ParentActivityId = 61,
                                        Ordering = 3,
                                CrateStorage=  crateStorage
                                    },
                                }
                            },

                            new ActionDO
                            {
                                Id = 62,
                                ParentActivityId = 59,
                                Ordering = 3,
                                CrateStorage=  crateStorage
                            },
                        },

                    }
                }
            };
            return curAction;
        }

        public static ActionDO TestActionStateActive()
        {
            var actionTemplate = FixtureData.TestActivityTemplateDO1();
            return new ActionDO
            {
                Id = 2,
                Name = "Action with state active",
                Ordering = 2,
                ActivityTemplateId = actionTemplate.Id,
                ActivityTemplate = actionTemplate,
                ActionState = ActionState.Active,
            };
        }

        public static ActionDO TestActionStateDeactive()
        {
            var actionTemplate = FixtureData.TestActivityTemplateDO1();
            return new ActionDO
            {
                Id = 2,
                Name = "Action with state deactive",
                Ordering = 2,
                ActivityTemplateId = actionTemplate.Id,
                ActivityTemplate = actionTemplate,
                ActionState = ActionState.Deactive,
            };
        }

        public static ActionDO TestActionStateError()
        {
            var actionTemplate = FixtureData.TestActivityTemplateDO1();
            return new ActionDO
            {
                Id = 2,
                Name = "Action with state error",
                Ordering = 2,
                ActivityTemplateId = actionTemplate.Id,
                ActivityTemplate = actionTemplate,
                ActionState = ActionState.Error,
            };
        }

        public static ActionDO TestActionStateInProcess()
        {
            var actionTemplate = FixtureData.TestActivityTemplateDO1();
            return new ActionDO
            {
                Id = 2,
                Name = "Action with state in-process",
                Ordering = 2,
                ActivityTemplateId = actionTemplate.Id,
                ActivityTemplate = actionTemplate,
                ActionState = ActionState.InProcess,
            };
        }

        public static ActionDO ConfigureTestAction57()
        {
            var actionTemplate = ActionTemplate();

            var processDo = new ContainerDO()
            {
                Id = 1,
                CrateStorage = EnvelopeIdCrateJson(),
                RouteId = TestRoute2().Id,
                ContainerState = 1
            };

            var actionDo = new ActionDO()
            {
                ActionState = ActionState.Unstarted,
                Name = "testaction",
                Id = 57,
                Ordering = 2,
                ParentActivityId = 54,
                ActivityTemplateId = actionTemplate.Id,
                ActivityTemplate = actionTemplate,
                CrateStorage = EnvelopeIdCrateJson()
            };

            return actionDo;
        }

        public static ActionDO ConfigureTestActionTree()
        {
            CrateStorageDTO crateStorageDTO = new CrateStorageDTO();
            crateStorageDTO.CrateDTO.Add(CreateStandardConfigurationControls());
            string crateStorage = JsonConvert.SerializeObject(crateStorageDTO);


            ActionDO curAction = new ActionDO()
            {
                Id = 1,
                Ordering = 1,
                CrateStorage = crateStorage,
                Activities = new List<ActivityDO>
                {
                    new ActionDO
                    {
                        Id = 23,
                        Ordering = 1,
                        ParentActivityId = 1,
                         CrateStorage=  crateStorage
                    },
                    new ActionDO
                    {
                        Id = 43,
                        ParentActivityId = 1,
                                        Ordering = 2,
                         CrateStorage=  crateStorage,
                        Activities = new List<ActivityDO>
                        {
                            new ActionDO
                            {
                                Id = 44,
                                Ordering = 1,
                                ParentActivityId = 43,
                                CrateStorage=  crateStorage
                                    },
                                    new ActionDO
                                    {
                                Id = 46,
                                Ordering = 2,
                                ParentActivityId = 43,
                                CrateStorage=  crateStorage
                            },
                            new ActionDO
                            {
                                Id = 48,
                                Ordering = 3,
                                ParentActivityId = 43,
                                CrateStorage=  crateStorage
                            },

                        }
                    },
                    new ActivityDO
                    {
                        Id = 52,
                        Ordering = 3,
                        ParentActivityId = 1,
                        Activities = new List<ActivityDO>
                        {
                            new ActionDO
                            {
                                Id = 53,
                                Ordering = 1,
                                ParentActivityId = 52,
                                CrateStorage=  crateStorage
                            },
                            new ActivityDO
                            {
                                Id = 54,
                                ParentActivityId = 52,
                                Ordering = 2,

                                Activities = new List<ActivityDO>
                                {
                                    new ActionDO
                                    {
                                        Id = 56,
                                        ParentActivityId = 54,
                                        Ordering = 1,
                                CrateStorage=  crateStorage
                                    },
                                    new ActionDO
                                    {
                                        Id = 57,
                                        ParentActivityId = 54,
                                        Ordering = 2
                                    },
                                    new ActionDO
                                    {
                                        Id = 58,
                                        ParentActivityId = 54,
                                        Ordering = 3,
                                CrateStorage=  crateStorage
                                    },

                                }
                            },
                            new ActionDO
                            {
                                Id = 55,
                                ParentActivityId = 52,
                                Ordering = 3,
                                CrateStorage=  crateStorage
                            },

                        }
                    },
                    new ActionDO
                    {
                        Id = 59,
                        Ordering = 4,
                        ParentActivityId = 1,
                         CrateStorage=  crateStorage,
                        Activities = new List<ActivityDO>
                        {
                            new ActionDO
                            {
                                Id = 60,
                                ParentActivityId = 59,
                                Ordering = 1,
                                CrateStorage=  crateStorage
                            },
                            new ActionDO
                            {
                                Id = 61,
                                ParentActivityId = 59,
                                Ordering = 2,
                                CrateStorage=  crateStorage,
                                Activities = new List<ActivityDO>
                                {
                                    new ActionDO
                                    {
                                        Id = 63,
                                        ParentActivityId = 61,
                                        Ordering = 1,
                                CrateStorage=  crateStorage
                                    },
                                    new ActionDO
                                    {
                                        Id = 64,
                                        ParentActivityId = 61,
                                        Ordering = 2,
                                CrateStorage=  crateStorage
                                    },
                                    new ActionDO
                                    {
                                        Id = 65,
                                        ParentActivityId = 61,
                                        Ordering = 3,
                                CrateStorage=  crateStorage
                                    },
                                }
                            },

                            new ActionDO
                            {
                                Id = 62,
                                ParentActivityId = 59,
                                Ordering = 3,
                                CrateStorage=  crateStorage
                            },
                        },

                    }
                }
            };

            FixParentActivityReferences(curAction);

            return curAction;
        }
        public static ActionDO TestActionTreeWithActionTemplates()
        {
            List<CrateDTO> curCratesDTO = FixtureData.TestCrateDTO1();
            CrateStorageDTO crateStorageDTO = new CrateStorageDTO();
            crateStorageDTO.CrateDTO.AddRange(curCratesDTO);
            string crateStorage = JsonConvert.SerializeObject(crateStorageDTO);
            var curActionTemplate = FixtureData.ActionTemplate();

            ActionDO curAction = new ActionDO()
            {
                Id = 1,
                Ordering = 1,
                CrateStorage = crateStorage,
                ActivityTemplate = curActionTemplate,
                Activities = new List<ActivityDO>
                {
                    new ActionDO
                    {
                        Id = 23,
                        Ordering = 1,
                        ParentActivityId = 1,
                        CrateStorage=  crateStorage,
                         ActivityTemplate = curActionTemplate,
                    },
                    new ActionDO
                    {
                        Id = 43,
                        ParentActivityId = 1,
                        Ordering = 2,
                        CrateStorage=  crateStorage,
                         ActivityTemplate = curActionTemplate,
                        Activities = new List<ActivityDO>
                        {
                            new ActionDO
                            {
                                Id = 44,
                                Ordering = 1,
                                ParentActivityId = 43,
                                CrateStorage=  crateStorage,
                         ActivityTemplate = curActionTemplate,
                            },
                            new ActionDO
                            {
                                Id = 46,
                                Ordering = 2,
                                ParentActivityId = 43,
                               CrateStorage=  crateStorage,
                         ActivityTemplate = curActionTemplate,
                            }
                        }
                    },
                    new ActivityDO
                    {
                        Id = 52,
                        Ordering = 3,
                        ParentActivityId = 1,


                    },
                    new ActionDO
                    {
                        Id = 59,
                        Ordering = 4,
                        ParentActivityId = 1,
CrateStorage=  crateStorage,
                         ActivityTemplate = curActionTemplate,
                        Activities = new List<ActivityDO>
                        {
                            new ActionDO
                            {
                                Id = 60,
                                ParentActivityId = 59,
                                Ordering = 1,
CrateStorage=  crateStorage,
                         ActivityTemplate = curActionTemplate,
                            },
                            new ActionDO
                            {
                                Id = 62,
                                ParentActivityId = 59,
                                Ordering = 3,
CrateStorage=  crateStorage,
                         ActivityTemplate = curActionTemplate,
                            }
                        }
                    }
                }
            };

            FixParentActivityReferences(curAction);
            return curAction;
        }

        public static ActionDO TestActionProcess()
        {
            var actionDo = new ActionDO
            {
                Id = 1,

                ActionState = ActionState.Unstarted,
                Name = "Action 1",
                CrateStorage = "config settings",
                ParentActivityId = 1,
                ActivityTemplateId = FixtureData.TestActivityTemplate1().Id
            };
            return actionDo;
        }

        public static ActionDO ConfigureTwilioAction()
        {
            var actionTemplate = FixtureData.TwilioActionTemplateDTO();

            var actionDO = new ActionDO()
            {
                Name = "testaction",
                Id = 57,
                ActivityTemplateId = actionTemplate.Id,
                ActivityTemplate = actionTemplate,
                CrateStorage = "",
            };

            return actionDO;
        }
    }
}