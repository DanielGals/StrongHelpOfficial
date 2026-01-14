using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Moq;
using StrongHelpOfficial.Controllers.Loaner;
using StrongHelpOfficial.Models;
using Xunit;

namespace StrongHelpOfficial.Tests.Controllers.Loaner;

public class ApplyForLoanControllerTests
{
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<IMemoryCache> _mockMemoryCache;
    private readonly ApplyForLoanController _controller;
    private readonly Mock<HttpContext> _mockHttpContext;
    private readonly Mock<ISession> _mockSession;

    public ApplyForLoanControllerTests()
    {
        _mockConfiguration = new Mock<IConfiguration>();
        _mockMemoryCache = new Mock<IMemoryCache>();
        _mockHttpContext = new Mock<HttpContext>();
        _mockSession = new Mock<ISession>();

        _mockHttpContext.Setup(x => x.Session).Returns(_mockSession.Object);

        _controller = new ApplyForLoanController(_mockConfiguration.Object, _mockMemoryCache.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = _mockHttpContext.Object
            }
        };
    }

    [Fact]
    public void SubmissionResult_SetsSuccessMessage_AndRedirects()
    {
        var model = new ApplyForLoanViewModel();

        var result = _controller.submissionResult(model);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal("ApplyForLoan", redirectResult.ControllerName);
    }

    [Fact]
    public void FailedSubmissionResult_SetsErrorMessage_AndRedirects()
    {
        var model = new ApplyForLoanViewModel();

        var result = _controller.failedSubmissionResult(model);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal("ApplyForLoan", redirectResult.ControllerName);
    }
}
