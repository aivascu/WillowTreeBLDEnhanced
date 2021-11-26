using ApprovalTests;
using ApprovalTests.Reporters;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using WillowTree.Services.DataAccess;

namespace WillowTreeSharp.ApprovalTests
{
    [UseReporter(typeof(DiffReporter))]
    [TestClass]
    public class FileDeserializationTests
    {
        [TestMethod]
        public void CanReadExtendedSaveFiles()
        {
            var ws = WillowSaveGameSerializer.ReadFile(@".\ReadTest\Extended\Save0001.sav", false);

            var json = JsonConvert.SerializeObject(ws);

            Approvals.VerifyJson(json);
        }
    }
}
