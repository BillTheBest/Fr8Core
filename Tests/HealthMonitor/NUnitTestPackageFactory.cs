﻿using System;
using NUnit.Core;

namespace HealthMonitor
{
    public class NUnitTestPackageFactory
    {
        public TestPackage CreateTestPackage()
        {
            var package = new TestPackage("Test");
            package.BasePath = Environment.CurrentDirectory;
            package.Assemblies.Add("Tests.dll");

            return package;
        }
    }
}
