using AutoSaveFile;
using EnvDTE;

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
            var options = new Mock<OptionPageGrid>();

            window.Setup(win => win.Document).Returns(document.Object);
            document.Setup(doc => doc.FullName).Returns("c:\\test\\tester.cs");
            options.Setup(opt => opt.IgnoredFileTypes).Returns("foo");
            
            // test here ..

            AutoSaveFilePackage.SaveMaybe(window.Object, options.Object, null);
        
        }

    }
}
