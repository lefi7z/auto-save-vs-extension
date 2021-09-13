using AutoSaveFile;
using EnvDTE;
using FluentAssertions;
using Moq;
using Xunit;

namespace AutoSaveFileTests
{
    public class HelperTests
    {
        [Fact]
        public void GetFileType_ReturnsDocumentExt_WhenDocumentPathIsAvailable()
        {
            var window = new Mock<Window>();
            var document = new Mock<Document>();

            window.Setup(win => win.Document).Returns(document.Object);
            document.Setup(doc => doc.FullName).Returns("c:\\test\\tester.cs");

            // test here ..
        }

    }
}
