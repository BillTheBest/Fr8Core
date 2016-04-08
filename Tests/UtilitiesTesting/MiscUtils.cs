﻿using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;

namespace DockyardTest.Utilities
{
    [TestFixture]
    public class MiscUtilsTest
    {
        [Test]
        public void MaskPassword_Should_MaskPasswordWithSemicolon()
        {
            string cs = "Data Source=.;Initial Catalog=DockyardDB2;Integrated Security=SSPI;Password=strong!password;";
            string cs_expected = "Data Source=.;Initial Catalog=DockyardDB2;Integrated Security=SSPI;Password=*****;";
            string masked = MiscUtils.MaskPassword(cs);
            Assert.AreEqual(cs_expected, masked);
        }

        [Test]
        public void MaskPassword_Should_MaskPasswordWithoutSemicolon()
        {
            string cs = "Data Source=.;Initial Catalog=DockyardDB2;Integrated Security=SSPI;Password=strong!password";
            string cs_expected = "Data Source=.;Initial Catalog=DockyardDB2;Integrated Security=SSPI;Password=*****";
            string masked = MiscUtils.MaskPassword(cs);
            Assert.AreEqual(cs_expected, masked);
        }

    }
}
