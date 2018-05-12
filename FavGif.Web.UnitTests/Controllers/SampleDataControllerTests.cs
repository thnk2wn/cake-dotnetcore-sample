using System;
using Xunit;
using FavGif.Web.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using static FavGif.Web.Controllers.SampleDataController;
using System.Linq;

namespace FavGif.Web.UnitTests.Controllers
{
    public class SampleDataControllerTests
    {
		[Fact]
        public void WeatherForecasts_ShouldReturnOneOrMore()
        {
            // Arrange
            var controller = new SampleDataController();

            // Act
			var result = controller.WeatherForecasts();

			// Assert
			var enu = result.Should().BeAssignableTo<IEnumerable<WeatherForecast>>().Subject;

			var list = enu.ToList();
			list.Should().NotBeNullOrEmpty();
        }
    }
}
