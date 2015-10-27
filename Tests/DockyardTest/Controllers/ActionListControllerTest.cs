﻿using System;
using System.Linq;
using System.Web.Http.Results;
using NUnit.Framework;
using StructureMap;
using Data.Entities;
using Data.Interfaces;
using Data.Interfaces.DataTransferObjects;
using Data.States;
using Hub.Services;
using UtilitiesTesting;
using UtilitiesTesting.Fixtures;
using Web.Controllers;
using Web.ViewModels;
using DockyardTest.Controllers.Api;

namespace DockyardTest.Controllers
{
    [TestFixture]
    [Category("ActionListController")]
    public class ActionListControllerTest : ApiControllerTestBase
    {
        private SubrouteDO _curSubroute;
        private ActionListController _actionListController;

        public override void SetUp()
        {
            base.SetUp();
            // DO-1214
            //InitializeActionList();
            _actionListController = CreateController<ActionListController>();
        }
        // DO-1214
//        [Test]
//        public void ActionListController_CanGetBySubrouteId()
//        {
//            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
//            {
//
//
//                var actionResult = _actionListController.GetBySubrouteId(
//                    _curSubroute.Id, ActionListType.Immediate);
//
//                var okResult = actionResult as OkNegotiatedContentResult<ActionListDTO>;
//
//                Assert.IsNotNull(okResult);
//                Assert.IsNotNull(okResult.Content);
//                Assert.AreEqual(okResult.Content.Id, _curActionList.Id);
//            }
//        }
//
//        #region Private methods
//        private void InitializeActionList()
//        {
//            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
//            {
//                //Add a template
//                var curRoute = FixtureData.TestRoute1();
//                uow.RouteRepository.Add(curRoute);
//                uow.SaveChanges();
//
//                _curSubroute = FixtureData.TestSubrouteDO1();
//                _curSubroute.ParentTemplateId = curRoute.Id;
//                uow.SubrouteRepository.Add(_curSubroute);
//                uow.SaveChanges();
//
//                /*_curSubroute = FixtureData.TestSubrouteDO1();
//                uow.SubrouteRepository.Add(_curSubroute);
//                uow.SaveChanges();*/
//
//                _curActionList = FixtureData.TestActionList();
//                _curActionList.ActionListType = ActionListType.Immediate;
//                _curActionList.CurrentActivity = null;
//                _curActionList.SubrouteID = _curSubroute.Id;
//
//                uow.ActionListRepository.Add(_curActionList);
//                uow.SaveChanges();
//            }
//        }
//
//        #endregion
    }

}
