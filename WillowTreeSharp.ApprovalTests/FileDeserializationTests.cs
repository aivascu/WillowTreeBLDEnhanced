using System.IO;
using ApprovalTests;
using ApprovalTests.Reporters;
using ApprovalTests.Reporters.TestFrameworks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using WillowTree.Services.DataAccess;
using WillowTreeSharp.Domain;

namespace WillowTreeSharp.ApprovalTests
{
    [UseReporter(typeof(MsTestReporter))]
    [TestClass]
    public class FileDeserializationTests
    {
        [TestMethod]
        public void CanReadVanillaSaveFiles()
        {
            var ws = WillowSaveGameSerializer.ReadFile(@".\ReadTest\Vanilla\Save0001.sav", false);

            var json = JsonConvert.SerializeObject(ws);

            Approvals.VerifyJson(json);
        }

        [TestMethod]
        public void CanReadExtendedSaveFiles()
        {
            var ws = WillowSaveGameSerializer.ReadFile(@".\ReadTest\Extended\Save0001.sav", false);

            var json = JsonConvert.SerializeObject(ws);

            Approvals.VerifyJson(json);
        }

        [TestMethod, Ignore]
        public void CanSerializeExtendedSaveFiles()
        {
            var text = File.ReadAllText(@".\WriteTest\Extended\Save0001.json");
            var saveGame = JsonConvert.DeserializeObject<WillowSaveGame>(text);

            var bytes = WillowSaveGameSerializer.Serialize(saveGame);

            Approvals.VerifyBinaryFile(bytes, "sav");
        }
    }
}
