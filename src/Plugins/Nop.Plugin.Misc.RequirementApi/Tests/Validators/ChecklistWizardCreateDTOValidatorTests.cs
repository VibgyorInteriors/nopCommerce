using FluentValidation.TestHelper;
using Nop.Plugin.Misc.RequirementApi.Models.DTO;
using Nop.Plugin.Misc.RequirementApi.Validators;
using NUnit.Framework;

namespace Nop.Plugin.Misc.RequirementApi.Tests.Validators;

[TestFixture]
public class ChecklistWizardCreateDTOValidatorTests
{
    private ChecklistWizardCreateDTOValidator _validator;

    [SetUp]
    public void Setup()
    {
        _validator = new ChecklistWizardCreateDTOValidator();
    }

    #region PrimaryName Tests

    [Test]
    public void ShouldHaveErrorWhenPrimaryNameIsNullOrEmpty()
    {
        var model = new ChecklistWizardCreateDTO
        {
            PrimaryName = null,
            MobileNumber = "1234567890",
            Email = "test@example.com"
        };
        _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.PrimaryName);

        model.PrimaryName = string.Empty;
        _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.PrimaryName);

        model.PrimaryName = "   ";
        _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.PrimaryName);
    }

    [Test]
    public void ShouldNotHaveErrorWhenPrimaryNameIsSpecified()
    {
        var model = new ChecklistWizardCreateDTO
        {
            PrimaryName = "John Doe",
            MobileNumber = "1234567890",
            Email = "test@example.com"
        };
        _validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.PrimaryName);
    }

    #endregion

    #region MobileNumber Tests

    [Test]
    public void ShouldHaveErrorWhenMobileNumberIsNullOrEmpty()
    {
        var model = new ChecklistWizardCreateDTO
        {
            PrimaryName = "John Doe",
            MobileNumber = null,
            Email = "test@example.com"
        };
        _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.MobileNumber);

        model.MobileNumber = string.Empty;
        _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.MobileNumber);

        model.MobileNumber = "   ";
        _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.MobileNumber);
    }

    [Test]
    public void ShouldNotHaveErrorWhenMobileNumberIsSpecified()
    {
        var model = new ChecklistWizardCreateDTO
        {
            PrimaryName = "John Doe",
            MobileNumber = "1234567890",
            Email = "test@example.com"
        };
        _validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.MobileNumber);
    }

    #endregion

    #region Email Tests

    [Test]
    public void ShouldHaveErrorWhenEmailIsNullOrEmpty()
    {
        var model = new ChecklistWizardCreateDTO
        {
            PrimaryName = "John Doe",
            MobileNumber = "1234567890",
            Email = null
        };
        _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.Email);

        model.Email = string.Empty;
        _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.Email);

        model.Email = "   ";
        _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Test]
    public void ShouldHaveErrorWhenEmailIsWrongFormat()
    {
        var model = new ChecklistWizardCreateDTO
        {
            PrimaryName = "John Doe",
            MobileNumber = "1234567890",
            Email = "invalid-email"
        };
        _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.Email);

        model.Email = "test@";
        _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.Email);

        model.Email = "@example.com";
        _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Test]
    public void ShouldNotHaveErrorWhenEmailIsCorrectFormat()
    {
        var model = new ChecklistWizardCreateDTO
        {
            PrimaryName = "John Doe",
            MobileNumber = "1234567890",
            Email = "test@example.com"
        };
        _validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.Email);

        model.Email = "user.name+tag@example.co.uk";
        _validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.Email);
    }

    #endregion

    #region FamilySize Tests

    [Test]
    public void ShouldNotHaveErrorWhenFamilySizeIsNullOrEmpty()
    {
        var model = new ChecklistWizardCreateDTO
        {
            PrimaryName = "John Doe",
            MobileNumber = "1234567890",
            Email = "test@example.com",
            FamilySize = null
        };
        _validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.FamilySize);

        model.FamilySize = string.Empty;
        _validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.FamilySize);
    }

    [Test]
    public void ShouldHaveErrorWhenFamilySizeIsInvalidNumber()
    {
        var model = new ChecklistWizardCreateDTO
        {
            PrimaryName = "John Doe",
            MobileNumber = "1234567890",
            Email = "test@example.com",
            FamilySize = "not-a-number"
        };
        _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.FamilySize);

        model.FamilySize = "abc123";
        _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.FamilySize);
    }

    [Test]
    public void ShouldNotHaveErrorWhenFamilySizeIsValidNumber()
    {
        var model = new ChecklistWizardCreateDTO
        {
            PrimaryName = "John Doe",
            MobileNumber = "1234567890",
            Email = "test@example.com",
            FamilySize = "4"
        };
        _validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.FamilySize);

        model.FamilySize = "10";
        _validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.FamilySize);
    }

    #endregion

    #region Optional Fields Tests

    [Test]
    public void ShouldNotHaveErrorWhenOptionalFieldsAreNullOrEmpty()
    {
        var model = new ChecklistWizardCreateDTO
        {
            PrimaryName = "John Doe",
            MobileNumber = "1234567890",
            Email = "test@example.com",
            PrimaryOccupation = null,
            SecondaryName = null,
            PropertyType = string.Empty,
            FamilySize = null
        };
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.PrimaryOccupation);
        result.ShouldNotHaveValidationErrorFor(x => x.SecondaryName);
        result.ShouldNotHaveValidationErrorFor(x => x.PropertyType);
        result.ShouldNotHaveValidationErrorFor(x => x.FamilySize);
    }

    #endregion
}

