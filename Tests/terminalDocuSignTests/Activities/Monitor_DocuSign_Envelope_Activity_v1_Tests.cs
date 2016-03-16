﻿using System.ComponentModel.DataAnnotations;
using System.Linq;
using Data.Control;
using Data.Crates;
using Data.Entities;
using Data.Interfaces.Manifests;
using Hub.Managers;
using NUnit.Framework;
using terminalDocuSign;
using terminalDocuSign.Actions;
using terminalDocuSignTests.Fixtures;
using UtilitiesTesting.Fixtures;

namespace terminalDocuSignTests.Activities
{
    //[TestFixture]
    //[Category("Monitor_DocuSign_Envelope_Activity_v1")]
    //public class Monitor_DocuSign_Envelope_Activity_v1_Tests : BaseTest
    //{
    //    [Test]
    //    public void ActivityIsValid_WhenIsNotConfigured_ReturnsFalse()
    //    {
    //        var target = new Monitor_DocuSign_Envelope_Activity_v1();
    //        var result = target.ValidateActivityInternal(FixtureData.TestActivity1());
    //        Assert.AreNotEqual(ValidationResult.Success, result);
    //        Assert.AreEqual(DocuSignValidationUtils.ControlsAreNotConfiguredErrorMessage, result.ErrorMessage);
    //    }

    //    [Test]
    //    public void ActivityIsValid_WhenNoNotificationIsSelected_ReturnsFalse()
    //    {
    //        var target = new Monitor_DocuSign_Envelope_Activity_v1(DocuSignActivityFixtureData.DocuSignManagerWithoutTemplates());
    //        var activityDO = FixtureData.TestActivity1();
    //        activityDO = target.Configure(activityDO, FixtureData.AuthToken_TerminalIntegration()).Result;
    //        SetRecipientConditionSelected(activityDO);
    //        SetRecipientText(activityDO);
    //        var result = target.ValidateActivityInternal(activityDO);
    //        Assert.AreNotEqual(ValidationResult.Success, result);
    //        Assert.AreEqual("At least one notification option must be selected", result.ErrorMessage);
    //    }

    //    [Test]
    //    public void ActivityIsValid_WhenNoEnvelopeConditionIsSelected_ReturnsFalse()
    //    {
    //        var target = new Monitor_DocuSign_Envelope_Activity_v1(DocuSignActivityFixtureData.DocuSignManagerWithoutTemplates());
    //        var activityDO = FixtureData.TestActivity1();
    //        activityDO = target.Configure(activityDO, FixtureData.AuthToken_TerminalIntegration()).Result;
    //        SetNotificationSelected(activityDO);
    //        var result = target.ValidateActivityInternal(activityDO);
    //        Assert.AreNotEqual(ValidationResult.Success, result);
    //        Assert.AreEqual("At least one envelope option must be selected", result.ErrorMessage);
    //    }

    //    [Test]
    //    public void ActivityIsValid_WhenTemplateMustBeSetButThereAreNoTemplates_ReturnsFalse()
    //    {
    //        var target = new Monitor_DocuSign_Envelope_Activity_v1(DocuSignActivityFixtureData.DocuSignManagerWithoutTemplates());
    //        var activityDO = FixtureData.TestActivity1();
    //        activityDO = target.Configure(activityDO, FixtureData.AuthToken_TerminalIntegration()).Result;
    //        SetNotificationSelected(activityDO);
    //        SetTemplateConditionSelected(activityDO);
    //        var result = target.ValidateActivityInternal(activityDO);
    //        Assert.AreNotEqual(ValidationResult.Success, result);
    //        Assert.AreEqual(DocuSignValidationUtils.NoTemplateExistsErrorMessage, result.ErrorMessage);
    //    }

    //    [Test]
    //    public void ActivityIsValid_WhenTemplateMustBeSetButItIsNot_ReturnsFalse()
    //    {
    //        var target = new Monitor_DocuSign_Envelope_Activity_v1(DocuSignActivityFixtureData.DocuSignManagerWithTemplates());
    //        var activityDO = FixtureData.TestActivity1();
    //        activityDO = target.Configure(activityDO, FixtureData.AuthToken_TerminalIntegration()).Result;
    //        SetNotificationSelected(activityDO);
    //        SetTemplateConditionSelected(activityDO);
    //        var result = target.ValidateActivityInternal(activityDO);
    //        Assert.AreNotEqual(ValidationResult.Success, result);
    //        Assert.AreEqual(DocuSignValidationUtils.TemplateIsNotSelectedErrorMessage, result.ErrorMessage);
    //    }
    //    [Test]
    //    public void ActivityIsValid_WhenAllFieldsAreSet_ReturnsTrue()
    //    {
    //        var target = new Monitor_DocuSign_Envelope_Activity_v1(DocuSignActivityFixtureData.DocuSignManagerWithTemplates());
    //        var activityDO = FixtureData.TestActivity1();
    //        activityDO = target.Configure(activityDO, FixtureData.AuthToken_TerminalIntegration()).Result;
    //        SetNotificationSelected(activityDO);
    //        SetTemplateConditionSelected(activityDO);
    //        SetTemplate(activityDO);
    //        var result = target.ValidateActivityInternal(activityDO);
    //        Assert.AreEqual(ValidationResult.Success, result);
    //    }

    //    private void SetNotificationSelected(ActivityDO activity)
    //    {
    //        using (var crateStorage = new CrateManager().GetUpdatableStorage(activity))
    //        {
    //            var configControls = crateStorage.FirstCrate<StandardConfigurationControlsCM>(c => true).Content;
    //            configControls.Controls
    //                .Where(c => c.Type == ControlTypes.CheckBox)
    //                .First(x => x.Name == "RecipientSigned")
    //                .Selected = true;
    //        }
    //    }

    //    private void SetRecipientConditionSelected(ActivityDO activity)
    //    {
    //        using (var crateStorage = new CrateManager().GetUpdatableStorage(activity))
    //        {
    //            var configControls = crateStorage.FirstCrate<StandardConfigurationControlsCM>(c => true).Content;
    //            configControls.Controls
    //                          .OfType<RadioButtonGroup>()
    //                          .First()
    //                          .Radios
    //                          .First(x => x.Name == "recipient")
    //                          .Selected = true;
    //        }
    //    }

    //    private void SetRecipientText(ActivityDO activity)
    //    {
    //        using (var crateStorage = new CrateManager().GetUpdatableStorage(activity))
    //        {
    //            var configControls = crateStorage.FirstCrate<StandardConfigurationControlsCM>(c => true).Content;
    //            configControls.Controls
    //                          .OfType<RadioButtonGroup>()
    //                          .First()
    //                          .Radios
    //                          .First(x => x.Name == "recipient")
    //                          .Controls
    //                          .First()
    //                          .Value = "foo@bar.com";
    //        }
    //    }

    //    private void SetTemplateConditionSelected(ActivityDO activity)
    //    {
    //        using (var crateStorage = new CrateManager().GetUpdatableStorage(activity))
    //        {
    //            var configControls = crateStorage.FirstCrate<StandardConfigurationControlsCM>(c => true).Content;
    //            configControls.Controls
    //                          .OfType<RadioButtonGroup>()
    //                          .First()
    //                          .Radios
    //                          .First(x => x.Name == "template")
    //                          .Selected = true;
    //        }
    //    }

    //    private void SetTemplate(ActivityDO activity)
    //    {
    //        using (var crateStorage = new CrateManager().GetUpdatableStorage(activity))
    //        {
    //            var configControls = crateStorage.FirstCrate<StandardConfigurationControlsCM>(c => true).Content;
    //            configControls.Controls
    //                          .OfType<RadioButtonGroup>()
    //                          .First()
    //                          .Radios
    //                          .First(x => x.Name == "template")
    //                          .Controls
    //                          .OfType<DropDownList>()
    //                          .First()
    //                          .selectedKey = "First";
    //        }
    //    }
    //}
}
