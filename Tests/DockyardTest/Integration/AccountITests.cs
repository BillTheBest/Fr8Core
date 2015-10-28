﻿using System;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using StructureMap;
using Data.Interfaces;
using Hub.Services;
using UtilitiesTesting;
using UtilitiesTesting.Fixtures;

namespace DockyardTest.Integration
{
    [TestFixture]
    [Category("IntegrationTests")]
    public class AccountITests : BaseTest
    {
        [Test]
        [Category("IntegrationTests")]
        public async void ITest_CanResetPassword()
        {
            string email;
            string id;
            // SETUP
            var account = ObjectFactory.GetInstance<Fr8Account>();
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var userDO = FixtureData.TestUser1();
                account.Create(uow, userDO);
                id = userDO.Id;
                email = userDO.EmailAddress.Address;
            }

            // EXECUTE
            // generate a forgot password email
            await account.ForgotPasswordAsync(email);
            // get callback url from generated email
            string callbackUrl;
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var envelopeDO = uow.EnvelopeRepository.GetQuery().Single();
                callbackUrl = (string)envelopeDO.MergeData["-callback_url-"];
            }
		    var userId = Regex.Match(callbackUrl,
		    				 "userId=(?<userId>[-a-f\\d]+)",
		    				 RegexOptions.IgnoreCase)
		        .Groups["userId"].Value;
		    var code = Regex.Match(callbackUrl,
		    				 "code=(?<code>[\\d]+)",
		    				 RegexOptions.IgnoreCase)
		        .Groups["code"].Value;
		    var result = await account.ResetPasswordAsync(userId, code, "123456");

            // VERIFY
		    Assert.AreEqual(id, userId);
		    Assert.IsTrue(result.Succeeded, string.Join(", ", result.Errors));
        }
    }
}
