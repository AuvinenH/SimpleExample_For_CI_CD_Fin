using FluentAssertions;
using SimpleExample.Domain.Entities;
using SimpleExample.Application.DTOs;
using Xunit;

namespace SimpleExample.Tests.Validators;

public class ValidatorTests
{
  [Fact]
public void Should_Have_Error_When_FirstName_Is_Empty()
{
    CreateUserDto dto = new CreateUserDto { FirstName = "", LastName = "Meikäläinen", Email = "test@test.com" };
    var result = _validator.TestValidate(dto);
    
    // VÄÄRÄ ODOTUS - testi epäonnistuu!
    result.ShouldNotHaveAnyValidationErrors();
}
    
}