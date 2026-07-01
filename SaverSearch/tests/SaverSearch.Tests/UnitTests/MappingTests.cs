using AutoMapper;
using SaverSearch.Application.Common.Mappings;
using SaverSearch.Application.Dtos;
using SaverSearch.Domain.Entities;
using Xunit;

namespace SaverSearch.Tests.UnitTests;

public class MappingTests
{
    private readonly IMapper _mapper;

    public MappingTests()
    {
        var config = new MapperConfiguration(
            cfg => cfg.AddProfile<MappingProfile>(),
            Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance
        );
        _mapper = config.CreateMapper();
    }

    [Fact]
    public void MapperConfiguration_ShouldBeValid()
    {
        var config = new MapperConfiguration(
            cfg => cfg.AddProfile<MappingProfile>(),
            Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance
        );
        config.AssertConfigurationIsValid();
    }

    [Fact]
    public void Category_ShouldMapToCategoryDto()
    {
        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = "Electronics",
            Description = "TVs and laptops",
            IsActive = true
        };

        var dto = _mapper.Map<CategoryDto>(category);

        Assert.Equal(category.Id, dto.Id);
        Assert.Equal(category.Name, dto.Name);
        Assert.Equal(category.Description, dto.Description);
        Assert.Equal(category.IsActive, dto.IsActive);
    }
}
