using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using StrongHelpOfficial.Controllers;
using Xunit;

namespace StrongHelpOfficial.Tests.Controllers;

public class HomeControllerTests
{
    private readonly Mock<ILogger<HomeController>> _mockLogger;
    private readonly HomeController _controller;

    public HomeControllerTests()
    {
        _mockLogger = new Mock<ILogger<HomeController>>();
        _controller = new HomeController(_mockLogger.Object);
    }

    [Fact]
    public void Index_ReturnsViewResult()
    {
        var result = _controller.Index();
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public void About_ReturnsViewResult()
    {
        var result = _controller.About();
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public void Contact_ReturnsViewResult()
    {
        var result = _controller.Contact();
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public void HelpCenter_ReturnsViewResult()
    {
        var result = _controller.HelpCenter();
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public void FAQs_ReturnsViewResult()
    {
        var result = _controller.FAQs();
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public void Terms_ReturnsViewResult()
    {
        var result = _controller.Terms();
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public void Privacy_ReturnsViewResult()
    {
        var result = _controller.Privacy();
        Assert.IsType<ViewResult>(result);
    }
}
