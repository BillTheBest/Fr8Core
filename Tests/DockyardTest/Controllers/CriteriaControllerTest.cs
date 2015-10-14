﻿using System.Web.Http.Results;
using NUnit.Framework;
using StructureMap;
using Data.Entities;
using Data.Interfaces;
using Data.Interfaces.DataTransferObjects;
using DockyardTest.Controllers.Api;
using UtilitiesTesting.Fixtures;
using Web.Controllers;

namespace DockyardTest.Controllers
{
    public class CriteriaControllerTest : ApiControllerTestBase
    {
        private SubrouteDO _curSubroute;
        private CriteriaDO _curCriteria;

        public override void SetUp()
        {
            base.SetUp();
            InitializeCriteria();
        }

        [Test]
        public void CriteriaController_GetBySubrouteId()
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var controller = CreateController<CriteriaController>();

                var actionResult = controller.GetBySubrouteId(_curSubroute.Id);

                var okResult = actionResult as OkNegotiatedContentResult<CriteriaDTO>;

                Assert.IsNotNull(okResult);
                Assert.IsNotNull(okResult.Content);
                Assert.AreEqual(okResult.Content.Id, _curCriteria.Id);
            }
        }

        private void InitializeCriteria()
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {

                //Add a template
                var curRoute = FixtureData.TestRoute1();
                uow.RouteRepository.Add(curRoute);
                uow.SaveChanges();

                //Add a template
                var route = FixtureData.TestRoute1();
                uow.RouteRepository.Add(route);
                uow.SaveChanges();
                //Add a processnodetemplate to processtemplate 
                _curSubroute = FixtureData.TestSubrouteDO1();
                _curSubroute.ParentActivityId = route.Id;
                uow.SubrouteRepository.Add(_curSubroute);
                uow.SaveChanges();

                /*_curSubroute = FixtureData.TestSubrouteDO1();
                uow.SubrouteRepository.Add(_curSubroute);
                uow.SaveChanges();*/

                _curCriteria = FixtureData.TestCriteria1();
                _curCriteria.Subroute = _curSubroute;

                uow.CriteriaRepository.Add(_curCriteria);
                uow.SaveChanges();
            }
        }
    }
}
